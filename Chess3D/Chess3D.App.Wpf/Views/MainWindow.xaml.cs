using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Chess3D.App.Wpf.Services;
using Chess3D.Core.Enums;
using Chess3D.Core.Models;
using Chess3D.Engine.Stockfish.Services;
using Chess3D.Rendering.Wpf.ViewModels;

namespace Chess3D.App.Wpf.Views;

public partial class MainWindow : Window
{
    private static readonly Point3D DefaultCameraTarget = new(0, 0.5, 0);
    private const double DefaultCameraDistance = 13.0;
    private const double DefaultCameraPitch = -35.0;
    private const double WhiteYaw = 180.0;
    private const double BlackYaw = 0.0;
    private const double DefaultCameraFov = 60.0;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    private readonly OrbitCameraController _cameraController;
    private BoardState _boardState;
    private Board3DViewModel _boardViewModel;

    private StockfishUciClient? _stockfishClient;
    private CancellationTokenSource? _cpuCts;
    private bool _isCpuThinking;
    private bool _isGameOver;

    private bool _playVsCpu = true;
    private int _cpuLevel = 3;
    private PieceColor _humanColor = PieceColor.White;

    private bool _isCpuAvailable = true;

    public MainWindow()
    {
        InitializeComponent();

        SourceInitialized += (_, _) => ApplyDarkTitleBar();

        _boardState = BoardState.CreateInitial();
        _boardViewModel = new Board3DViewModel(_boardState);

        Viewport.Children.Add(new ModelVisual3D
        {
            Content = _boardViewModel.Scene
        });

        _cameraController = new OrbitCameraController(Viewport, Camera);
        _cameraController.SetView(DefaultCameraTarget, DefaultCameraDistance, WhiteYaw, DefaultCameraPitch, DefaultCameraFov);

        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;

        UpdateMenuChecks();
        RefreshStatusBar();
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int dwAttribute,
        ref int pvAttribute,
        int cbAttribute);

    private void ApplyDarkTitleBar()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero)
            return;

        int enabled = 1;
        _ = DwmSetWindowAttribute(
            hwnd,
            DWMWA_USE_IMMERSIVE_DARK_MODE,
            ref enabled,
            Marshal.SizeOf<int>());
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await InitializeStockfishAsync();
        RefreshStatusBar();
        await TryPlayCpuMoveAsync();
    }

    private async void MainWindow_Closed(object? sender, EventArgs e)
    {
        _cpuCts?.Cancel();
        _cpuCts?.Dispose();

        if (_stockfishClient is not null)
            await _stockfishClient.DisposeAsync();
    }

    private async Task InitializeStockfishAsync()
    {
        if (!_playVsCpu)
            return;

        if (_stockfishClient is not null)
            return;

        string enginePath = Path.Combine(AppContext.BaseDirectory, "engines", "stockfish.exe");
        if (!File.Exists(enginePath))
        {
            _isCpuAvailable = false;

            MessageBox.Show(
                $"Stockfish introuvable : {enginePath}",
                "Stockfish",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            _playVsCpu = false;
            UpdateMenuChecks();
            RefreshStatusBar("Mode local — Stockfish introuvable");
            return;
        }

        _stockfishClient = new StockfishUciClient(enginePath);
        await _stockfishClient.InitializeAsync();
        await _stockfishClient.NewGameAsync();
    }

    private void Root_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_isGameOver)
        {
            RefreshStatusBar("Partie terminée");
            return;
        }

        if (_isCpuThinking)
        {
            RefreshStatusBar("Le CPU réfléchit...");
            return;
        }

        if (_playVsCpu && _boardState.SideToMove != _humanColor)
        {
            RefreshStatusBar("Ce n'est pas à vous de jouer");
            return;
        }

        Point mousePosition = e.GetPosition(Viewport);
        HitTestResult result = VisualTreeHelper.HitTest(Viewport, mousePosition);
        if (result is not RayHitTestResult rayResult)
            return;

        if (rayResult.ModelHit is GeometryModel3D geometryModel)
        {
            if (_boardViewModel.TrySelectPiece(geometryModel))
            {
                var piece = _boardViewModel.SelectedPiece!;
                UpdateSelectionText($"Sélection : {piece.PieceType} {piece.PieceColor} en {(char)('a' + piece.File)}{piece.Rank + 1}");
                RefreshStatusBar();
                return;
            }
        }

        Point3D hitPoint = rayResult.PointHit;
        if (!_boardViewModel.TryGetSquareFromHit(hitPoint, out int file, out int rank))
            return;

        bool movePlayed = false;

        if (_boardViewModel.RequiresPromotion(file, rank))
        {
            var promotionWindow = new PromotionWindow
            {
                Owner = this
            };

            bool? dialogResult = promotionWindow.ShowDialog();

            if (dialogResult == true && promotionWindow.SelectedPromotion.HasValue)
            {
                movePlayed = _boardViewModel.TryMoveSelectedPieceTo(file, rank, promotionWindow.SelectedPromotion.Value);

                if (movePlayed)
                {
                    UpdateSelectionText("Aucune sélection");
                    RefreshStatusBar($"Promotion vers {(char)('a' + file)}{rank + 1}");
                    UpdateCameraAfterMove();
                }
            }

            if (!movePlayed)
            {
                RefreshStatusBar("Promotion annulée");
                return;
            }
        }
        else
        {
            movePlayed = _boardViewModel.TryMoveSelectedPieceTo(file, rank);

            if (!movePlayed)
            {
                RefreshStatusBar($"Coup invalide vers {(char)('a' + file)}{rank + 1}");
                return;
            }

            UpdateSelectionText("Aucune sélection");
            RefreshStatusBar($"Déplacement vers {(char)('a' + file)}{rank + 1}");
            UpdateCameraAfterMove();
        }

        if (ShowGameStateIfNeeded())
            return;

        RefreshStatusBar();
        _ = TryPlayCpuMoveAsync();
    }

    private bool ShowGameStateIfNeeded()
    {
        var gameState = _boardState.GetGameEndState();

        switch (gameState)
        {
            case GameEndState.Check:
                RefreshStatusBar($"Échec sur les {(_boardState.SideToMove == PieceColor.White ? "blancs" : "noirs")}");
                return false;

            case GameEndState.Checkmate:
                {
                    _isGameOver = true;
                    string winner = _boardState.SideToMove == PieceColor.White ? "Noirs" : "Blancs";
                    RefreshStatusBar($"Échec et mat — victoire des {winner.ToLowerInvariant()}");
                    MessageBox.Show(
                        $"Échec et mat !\n\nVictoire des {winner}.",
                        "Fin de partie",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return true;
                }

            case GameEndState.Stalemate:
                _isGameOver = true;
                RefreshStatusBar("Pat — partie nulle");
                MessageBox.Show(
                    "Pat !\n\nLa partie est nulle.",
                    "Fin de partie",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return true;

            case GameEndState.None:
            default:
                return false;
        }
    }

    private async Task TryPlayCpuMoveAsync()
    {
        if (!_playVsCpu || _stockfishClient is null)
            return;

        if (_isGameOver || _isCpuThinking)
            return;

        if (_boardState.SideToMove == _humanColor)
            return;

        _isCpuThinking = true;
        RefreshStatusBar();

        _cpuCts?.Cancel();
        _cpuCts?.Dispose();
        _cpuCts = new CancellationTokenSource();

        try
        {
            string fen = _boardState.ToFen();
            Move cpuMove = await _stockfishClient.GetBestMoveAsync(fen, _cpuLevel, _cpuCts.Token);

            _boardState.MakeMove(cpuMove);
            _boardViewModel.RefreshPiecesFromBoardState();
            UpdateSelectionText("Aucune sélection");

            if (ShowGameStateIfNeeded())
                return;

            RefreshStatusBar($"CPU joue {cpuMove.ToUci()}");
        }
        catch (OperationCanceledException)
        {
            RefreshStatusBar("Calcul CPU annulé");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Erreur CPU/Stockfish : {ex.Message}",
                "Erreur",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            RefreshStatusBar("Erreur CPU/Stockfish");
        }
        finally
        {
            _isCpuThinking = false;
            RefreshStatusBar();
        }
    }

    private async void NewGame_Click(object sender, RoutedEventArgs e)
    {
        await StartNewGameAsync();
    }

    private async Task StartNewGameAsync()
    {
        _cpuCts?.Cancel();
        _cpuCts?.Dispose();
        _cpuCts = null;

        _isCpuThinking = false;
        _isGameOver = false;

        _boardState = BoardState.CreateInitial();
        _boardViewModel = new Board3DViewModel(_boardState);

        Viewport.Children.Clear();
        Viewport.Children.Add(new ModelVisual3D
        {
            Content = _boardViewModel.Scene
        });

        ResetCameraForCurrentMode();
        _boardViewModel.ClearHighlights();
        UpdateSelectionText("Aucune sélection");

        if (_stockfishClient is not null)
            await _stockfishClient.NewGameAsync();

        UpdateMenuChecks();
        RefreshStatusBar();
        await TryPlayCpuMoveAsync();
    }

    private void ResetCameraForCurrentMode()
    {
        if (_playVsCpu)
            AnimateCameraToSide(_humanColor, true);
        else
            AnimateCameraToSide(_boardState.SideToMove, true);
    }

    private void UpdateCameraAfterMove()
    {
        if (_playVsCpu)
            return;

        AnimateCameraToSide(_boardState.SideToMove, false);
    }

    private void AnimateCameraToSide(PieceColor side, bool resetView)
    {
        double yaw = side == PieceColor.White ? WhiteYaw : BlackYaw;
        int durationMs = resetView ? 1400 : 1100;

        _cameraController.AnimateToView(
            DefaultCameraTarget,
            DefaultCameraDistance,
            yaw,
            DefaultCameraPitch,
            DefaultCameraFov,
            durationMs);
    }

    private void UpdateMenuChecks()
    {
        VersusCpuMenuItem.IsChecked = _playVsCpu;
        LocalTwoPlayersMenuItem.IsChecked = !_playVsCpu;

        PlayWhiteMenuItem.IsChecked = _humanColor == PieceColor.White;
        PlayBlackMenuItem.IsChecked = _humanColor == PieceColor.Black;

        PlayWhiteMenuItem.IsEnabled = _playVsCpu && _isCpuAvailable;
        PlayBlackMenuItem.IsEnabled = _playVsCpu && _isCpuAvailable;

        CpuLevel1MenuItem.IsChecked = _cpuLevel == 1;
        CpuLevel2MenuItem.IsChecked = _cpuLevel == 2;
        CpuLevel3MenuItem.IsChecked = _cpuLevel == 3;
        CpuLevel4MenuItem.IsChecked = _cpuLevel == 4;
        CpuLevel5MenuItem.IsChecked = _cpuLevel == 5;
        CpuLevel6MenuItem.IsChecked = _cpuLevel == 6;
        CpuLevel7MenuItem.IsChecked = _cpuLevel == 7;
        CpuLevel8MenuItem.IsChecked = _cpuLevel == 8;
        CpuLevel9MenuItem.IsChecked = _cpuLevel == 9;
        CpuLevel10MenuItem.IsChecked = _cpuLevel == 10;

        CpuLevel1MenuItem.IsEnabled = _playVsCpu && _isCpuAvailable;
        CpuLevel2MenuItem.IsEnabled = _playVsCpu && _isCpuAvailable;
        CpuLevel3MenuItem.IsEnabled = _playVsCpu && _isCpuAvailable;
        CpuLevel4MenuItem.IsEnabled = _playVsCpu && _isCpuAvailable;
        CpuLevel5MenuItem.IsEnabled = _playVsCpu && _isCpuAvailable;
        CpuLevel6MenuItem.IsEnabled = _playVsCpu && _isCpuAvailable;
        CpuLevel7MenuItem.IsEnabled = _playVsCpu && _isCpuAvailable;
        CpuLevel8MenuItem.IsEnabled = _playVsCpu && _isCpuAvailable;
        CpuLevel9MenuItem.IsEnabled = _playVsCpu && _isCpuAvailable;
        CpuLevel10MenuItem.IsEnabled = _playVsCpu && _isCpuAvailable;
    }

    private void UpdateSelectionText(string text)
    {
        if (SelectionTextBlock != null)
            SelectionTextBlock.Text = text;
    }

    private void RefreshStatusBar(string? transientMessage = null)
    {
        string sideToMove = _boardState.SideToMove == PieceColor.White ? "Blancs" : "Noirs";

        if (StatusTextBlock != null)
        {
            StatusTextBlock.Text = string.IsNullOrWhiteSpace(transientMessage)
                ? $"Tour : {sideToMove}"
                : transientMessage!;
        }

        if (ViewModeTextBlock != null)
        {
            if (_playVsCpu)
            {
                string human = _humanColor == PieceColor.White ? "Blancs" : "Noirs";
                string modeText = _isCpuThinking
                    ? $"Mode : CPU — Vous jouez {human} — Niveau {_cpuLevel} — réflexion"
                    : $"Mode : CPU — Vous jouez {human} — Niveau {_cpuLevel}";
                ViewModeTextBlock.Text = modeText;
            }
            else
            {
                ViewModeTextBlock.Text = "Mode : 2 joueurs locaux";
            }
        }
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        string versionText = version is null
            ? "Version inconnue"
            : $"Version {version.Major}.{version.Minor}.{version.Build}";

        MessageBox.Show(
            "Chess 3D - " + $"{versionText}\n\n" +
            "Application d'échecs 3D avec moteur Stockfish.\n" +
            "Fonctionnalités : partie locale, partie contre CPU, affichage 3D, règles spéciales.\n\n" +
            "©2026 Etienne Jacquemoud",
            "A propos",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void Quit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void VersusCpuMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _playVsCpu = true;
        await InitializeStockfishAsync();

        if (!_isCpuAvailable)
        {
            _playVsCpu = false;
            UpdateMenuChecks();
            RefreshStatusBar("Mode local — Stockfish indisponible");
            return;
        }

        UpdateMenuChecks();
        await StartNewGameAsync();
    }

    private async void LocalTwoPlayersMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _playVsCpu = false;
        UpdateMenuChecks();
        await StartNewGameAsync();
    }

    private async void PlayWhiteMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (!_playVsCpu)
            return;

        _humanColor = PieceColor.White;
        UpdateMenuChecks();
        await StartNewGameAsync();
    }

    private async void PlayBlackMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (!_playVsCpu)
            return;

        _humanColor = PieceColor.Black;
        UpdateMenuChecks();
        await StartNewGameAsync();
    }

    private async void CpuLevel1MenuItem_Click(object sender, RoutedEventArgs e)
    {
        _cpuLevel = 1;
        UpdateMenuChecks();
        await StartNewGameAsync();
    }

    private async void CpuLevel2MenuItem_Click(object sender, RoutedEventArgs e)
    {
        _cpuLevel = 2;
        UpdateMenuChecks();
        await StartNewGameAsync();
    }

    private async void CpuLevel3MenuItem_Click(object sender, RoutedEventArgs e)
    {
        _cpuLevel = 3;
        UpdateMenuChecks();
        await StartNewGameAsync();
    }

    private async void CpuLevel4MenuItem_Click(object sender, RoutedEventArgs e)
    {
        _cpuLevel = 4;
        UpdateMenuChecks();
        await StartNewGameAsync();
    }

    private async void CpuLevel5MenuItem_Click(object sender, RoutedEventArgs e)
    {
        _cpuLevel = 5;
        UpdateMenuChecks();
        await StartNewGameAsync();
    }

    private async void CpuLevel6MenuItem_Click(object sender, RoutedEventArgs e)
    {
        _cpuLevel = 6;
        UpdateMenuChecks();
        await StartNewGameAsync();
    }

    private async void CpuLevel7MenuItem_Click(object sender, RoutedEventArgs e)
    {
        _cpuLevel = 7;
        UpdateMenuChecks();
        await StartNewGameAsync();
    }

    private async void CpuLevel8MenuItem_Click(object sender, RoutedEventArgs e)
    {
        _cpuLevel = 8;
        UpdateMenuChecks();
        await StartNewGameAsync();
    }

    private async void CpuLevel9MenuItem_Click(object sender, RoutedEventArgs e)
    {
        _cpuLevel = 9;
        UpdateMenuChecks();
        await StartNewGameAsync();
    }

    private async void CpuLevel10MenuItem_Click(object sender, RoutedEventArgs e)
    {
        _cpuLevel = 10;
        UpdateMenuChecks();
        await StartNewGameAsync();
    }
}