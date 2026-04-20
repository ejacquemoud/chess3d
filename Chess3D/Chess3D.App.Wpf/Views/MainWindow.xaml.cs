using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using Chess3D.App.Wpf.Services;
using Chess3D.Core.Enums;
using Chess3D.Core.Models;
using Chess3D.Engine.Stockfish.Services;
using Chess3D.Rendering.Wpf.ViewModels;

namespace Chess3D.App.Wpf.Views;

public partial class MainWindow : Window
{
    private enum BoardViewMode
    {
        LocalTwoPlayers,
        VersusCpu
    }

    private enum CpuLevel
    {
        Level1,
        Level2,
        Level3
    }

    private readonly record struct CameraViewTarget(Point3D Position, Point3D Target, double FieldOfView);

    private readonly OrbitCameraController _cameraController;
    private readonly Random _random = new();

    private BoardState _boardState = null!;
    private Board3DViewModel _boardViewModel = null!;
    private ModelVisual3D? _sceneVisual;

    private BoardViewMode _boardViewMode = BoardViewMode.LocalTwoPlayers;
    private PieceColor _humanPlayerColor = PieceColor.White;
    private CpuLevel _cpuLevel = CpuLevel.Level1;
    private bool _isCpuThinking;

    private StockfishUciClient? _stockfishClient;
    private CancellationTokenSource? _cpuCts;

    public MainWindow()
    {
        InitializeComponent();

        SourceInitialized += MainWindow_SourceInitialized;
        Closed += MainWindow_Closed;

        _cameraController = new OrbitCameraController(Viewport, Camera);

        StartNewGame();

        Root.MouseLeftButtonDown += Root_MouseLeftButtonDown;
    }

    private async void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        EnableDarkTitleBar();
        await InitializeStockfishAsync();
    }

    private async void MainWindow_Closed(object? sender, EventArgs e)
    {
        _cpuCts?.Cancel();
        _cpuCts?.Dispose();
        _cpuCts = null;

        if (_stockfishClient is not null)
            await _stockfishClient.DisposeAsync();
    }

    private async Task InitializeStockfishAsync()
    {
        if (_stockfishClient is not null)
            return;

        string enginePath = Path.Combine(AppContext.BaseDirectory, "Engines", "stockfish.exe");
        if (!File.Exists(enginePath))
        {
            MessageBox.Show($"Stockfish introuvable : {enginePath}", "Stockfish", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _stockfishClient = new StockfishUciClient(enginePath);

        try
        {
            await _stockfishClient.InitializeAsync();
            await _stockfishClient.NewGameAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Impossible d'initialiser Stockfish : {ex.Message}", "Stockfish", MessageBoxButton.OK, MessageBoxImage.Error);
            await _stockfishClient.DisposeAsync();
            _stockfishClient = null;
        }
    }

    private void EnableDarkTitleBar()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero)
            return;

        int enabled = 1;

        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 18985))
        {
            _ = DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref enabled, sizeof(int));
        }
        else if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
        {
            _ = DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref enabled, sizeof(int));
        }
    }

    private void Quit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void Root_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_isCpuThinking || IsCpuTurn())
            return;

        Point mousePosition = e.GetPosition(Viewport);

        HitTestResult result = VisualTreeHelper.HitTest(Viewport, mousePosition);
        if (result is not RayHitTestResult rayResult)
            return;

        if (rayResult.ModelHit is GeometryModel3D geometryModel)
        {
            if (_boardViewModel.TrySelectPiece(geometryModel))
            {
                var piece = _boardViewModel.SelectedPiece!;
                Title = $"Chess 3D - pièce sélectionnée : {piece.PieceType} {piece.PieceColor} en {(char)('a' + piece.File)}{piece.Rank + 1}";
                UpdateStatusDisplay();
                return;
            }
        }

        Point3D hitPoint = rayResult.PointHit;

        if (!_boardViewModel.TryGetSquareFromHit(hitPoint, out int file, out int rank))
            return;

        if (_boardViewModel.RequiresPromotion(file, rank))
        {
            var promotionWindow = new PromotionWindow
            {
                Owner = this
            };

            bool? dialogResult = promotionWindow.ShowDialog();

            if (dialogResult == true && promotionWindow.SelectedPromotion.HasValue)
            {
                if (_boardViewModel.TryMoveSelectedPieceTo(file, rank, promotionWindow.SelectedPromotion.Value))
                {
                    Title = $"Chess 3D - promotion vers {(char)('a' + file)}{rank + 1}";
                    OnMoveCompleted();
                    return;
                }
            }

            _boardViewModel.ClearHighlights();
            Title = $"Chess 3D - case : {(char)('a' + file)}{rank + 1}";
            UpdateStatusDisplay();
            return;
        }

        if (_boardViewModel.TryMoveSelectedPieceTo(file, rank))
        {
            Title = $"Chess 3D - déplacement vers {(char)('a' + file)}{rank + 1}";
            OnMoveCompleted();
        }
        else
        {
            _boardViewModel.ClearHighlights();
            Title = $"Chess 3D - case : {(char)('a' + file)}{rank + 1}";
            UpdateStatusDisplay();
        }
    }

    private void NewGame_Click(object sender, RoutedEventArgs e)
    {
        StartNewGame();
    }

    private void LocalTwoPlayersMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _boardViewMode = BoardViewMode.LocalTwoPlayers;

        LocalTwoPlayersMenuItem.IsChecked = true;
        VersusCpuMenuItem.IsChecked = false;

        AnimateCameraForCurrentMode();
        UpdateStatusDisplay();
    }

    private void VersusCpuMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _boardViewMode = BoardViewMode.VersusCpu;

        LocalTwoPlayersMenuItem.IsChecked = false;
        VersusCpuMenuItem.IsChecked = true;

        AnimateCameraForCurrentMode();
        UpdateStatusDisplay();
        TryStartCpuTurnIfNeeded();
    }

    private void PlayWhiteMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _humanPlayerColor = PieceColor.White;

        PlayWhiteMenuItem.IsChecked = true;
        PlayBlackMenuItem.IsChecked = false;

        if (_boardViewMode == BoardViewMode.VersusCpu)
            AnimateCameraForCurrentMode();

        UpdateStatusDisplay();
        TryStartCpuTurnIfNeeded();
    }

    private void PlayBlackMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _humanPlayerColor = PieceColor.Black;

        PlayWhiteMenuItem.IsChecked = false;
        PlayBlackMenuItem.IsChecked = true;

        if (_boardViewMode == BoardViewMode.VersusCpu)
            AnimateCameraForCurrentMode();

        UpdateStatusDisplay();
        TryStartCpuTurnIfNeeded();
    }

    private void CpuLevel1MenuItem_Click(object sender, RoutedEventArgs e)
    {
        _cpuLevel = CpuLevel.Level1;
        UpdateCpuLevelMenuChecks();
        UpdateStatusDisplay();
        TryStartCpuTurnIfNeeded();
    }

    private void CpuLevel2MenuItem_Click(object sender, RoutedEventArgs e)
    {
        _cpuLevel = CpuLevel.Level2;
        UpdateCpuLevelMenuChecks();
        UpdateStatusDisplay();
    }

    private void CpuLevel3MenuItem_Click(object sender, RoutedEventArgs e)
    {
        _cpuLevel = CpuLevel.Level3;
        UpdateCpuLevelMenuChecks();
        UpdateStatusDisplay();
    }

    private void UpdateCpuLevelMenuChecks()
    {
        CpuLevel1MenuItem.IsChecked = _cpuLevel == CpuLevel.Level1;
        CpuLevel2MenuItem.IsChecked = _cpuLevel == CpuLevel.Level2;
        CpuLevel3MenuItem.IsChecked = _cpuLevel == CpuLevel.Level3;
    }

    private int GetCpuLevelNumber()
    {
        return _cpuLevel switch
        {
            CpuLevel.Level1 => 1,
            CpuLevel.Level2 => 2,
            CpuLevel.Level3 => 3,
            _ => 1
        };
    }

    private void StartNewGame()
    {
        _cpuCts?.Cancel();
        _cpuCts?.Dispose();
        _cpuCts = null;

        _boardState = BoardState.CreateInitial();
        _boardViewModel = new Board3DViewModel(_boardState);
        _isCpuThinking = false;

        RenderScene();
        SetCameraInstantForCurrentMode();
        UpdateCpuLevelMenuChecks();

        Title = "Chess 3D - nouvelle partie";
        UpdateStatusDisplay();
        TryStartCpuTurnIfNeeded();
    }

    private void RenderScene()
    {
        if (_sceneVisual != null)
            Viewport.Children.Remove(_sceneVisual);

        _sceneVisual = new ModelVisual3D
        {
            Content = _boardViewModel.Scene
        };

        Viewport.Children.Add(_sceneVisual);
    }

    private PieceColor GetViewSide()
    {
        return _boardViewMode switch
        {
            BoardViewMode.LocalTwoPlayers => _boardState.SideToMove,
            BoardViewMode.VersusCpu => _humanPlayerColor,
            _ => _boardState.SideToMove
        };
    }

    private PieceColor GetCpuColor()
    {
        return _humanPlayerColor == PieceColor.White
            ? PieceColor.Black
            : PieceColor.White;
    }

    private bool IsCpuTurn()
    {
        return _boardViewMode == BoardViewMode.VersusCpu &&
               _boardState.SideToMove == GetCpuColor();
    }

    private CameraViewTarget GetCameraViewTargetForCurrentMode()
    {
        Point3D target = new Point3D(0, 0.5, 0);
        var viewSide = GetViewSide();

        Point3D position = viewSide == PieceColor.White
            ? new Point3D(0, 7.5, -10.5)
            : new Point3D(0, 7.5, 10.5);

        return new CameraViewTarget(position, target, 60);
    }

    private void SetCameraInstantForCurrentMode()
    {
        var view = GetCameraViewTargetForCurrentMode();

        Camera.BeginAnimation(PerspectiveCamera.PositionProperty, null);
        Camera.BeginAnimation(PerspectiveCamera.LookDirectionProperty, null);
        Camera.BeginAnimation(PerspectiveCamera.FieldOfViewProperty, null);

        Camera.Position = view.Position;
        Camera.LookDirection = view.Target - view.Position;
        Camera.UpDirection = new Vector3D(0, 1, 0);
        Camera.FieldOfView = view.FieldOfView;
    }

    private void AnimateCameraForCurrentMode()
    {
        var view = GetCameraViewTargetForCurrentMode();
        Vector3D newLookDirection = view.Target - view.Position;

        var duration = TimeSpan.FromMilliseconds(1400);
        var easing = new CubicEase { EasingMode = EasingMode.EaseInOut };

        var positionAnimation = new Point3DAnimation
        {
            To = view.Position,
            Duration = duration,
            EasingFunction = easing
        };

        var lookDirectionAnimation = new Vector3DAnimation
        {
            To = newLookDirection,
            Duration = duration,
            EasingFunction = easing
        };

        var fovAnimation = new DoubleAnimation
        {
            To = view.FieldOfView,
            Duration = duration,
            EasingFunction = easing
        };

        Camera.BeginAnimation(PerspectiveCamera.PositionProperty, positionAnimation);
        Camera.BeginAnimation(PerspectiveCamera.LookDirectionProperty, lookDirectionAnimation);
        Camera.BeginAnimation(PerspectiveCamera.FieldOfViewProperty, fovAnimation);
    }

    private void OnMoveCompleted()
    {
        _boardViewModel.ClearHighlights();
        AnimateCameraForCurrentMode();
        UpdateStatusDisplay();
        ShowGameStatePopupIfNeeded();
        TryStartCpuTurnIfNeeded();
    }

    private void TryStartCpuTurnIfNeeded()
    {
        if (_isCpuThinking)
            return;

        if (!IsCpuTurn())
            return;

        if (_cpuLevel != CpuLevel.Level1)
            return;

        if (!HasAnyMoveForSideToMove())
        {
            UpdateStatusDisplay();
            ShowGameStatePopupIfNeeded();
            return;
        }

        _isCpuThinking = true;
        UpdateStatusDisplay();

        _ = StartCpuMoveAsync();
    }

    private async Task StartCpuMoveAsync()
    {
        _cpuCts?.Cancel();
        _cpuCts?.Dispose();
        _cpuCts = new CancellationTokenSource();

        try
        {
            if (_stockfishClient is null)
            {
                await InitializeStockfishAsync();
                if (_stockfishClient is null)
                    throw new InvalidOperationException("Stockfish non initialisé.");
            }

            string fen = _boardState.ToFen();
            Move selectedMove = await _stockfishClient.GetBestMoveAsync(fen, GetCpuLevelNumber(), _cpuCts.Token);

            _boardState.MakeMove(selectedMove);
            _boardViewModel.RefreshPiecesFromBoardState();

            Title = $"Chess 3D - CPU joue {selectedMove.ToUci()}";

            _isCpuThinking = false;
            OnMoveCompleted();
        }
        catch (OperationCanceledException)
        {
            _isCpuThinking = false;
            UpdateStatusDisplay();
        }
        catch (Exception ex)
        {
            _isCpuThinking = false;
            UpdateStatusDisplay();

            MessageBox.Show(
                $"Erreur Stockfish : {ex.Message}",
                "Stockfish",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private bool HasAnyMoveForSideToMove()
    {
        foreach (var entry in _boardState.GetOccupiedSquares())
        {
            if (entry.Piece.Color != _boardState.SideToMove)
                continue;

            var moves = _boardState.GeneratePseudoLegalMovesFor(entry.Square);
            if (moves.Count > 0)
                return true;
        }

        return false;
    }

    private void UpdateStatusDisplay()
    {
        string sideToMoveText = _boardState.SideToMove == PieceColor.White
            ? "Blancs"
            : "Noirs";

        if (StatusTextBlock != null)
            StatusTextBlock.Text = $"Tour : {sideToMoveText}";

        if (SelectionTextBlock != null)
        {
            if (_boardViewModel.SelectedPiece == null)
            {
                SelectionTextBlock.Text = "Aucune sélection";
            }
            else
            {
                var piece = _boardViewModel.SelectedPiece;
                SelectionTextBlock.Text =
                    $"Sélection : {piece.PieceType} {piece.PieceColor} en {(char)('a' + piece.File)}{piece.Rank + 1}";
            }
        }

        if (ViewModeTextBlock != null)
        {
            string modeText = _boardViewMode switch
            {
                BoardViewMode.LocalTwoPlayers => "Mode : 2 joueurs",
                BoardViewMode.VersusCpu => _humanPlayerColor == PieceColor.White
                    ? $"Mode : CPU niv. {GetCpuLevelNumber()} - vous jouez les Blancs"
                    : $"Mode : CPU niv. {GetCpuLevelNumber()} - vous jouez les Noirs",
                _ => "Mode : inconnu"
            };

            if (_isCpuThinking && _boardViewMode == BoardViewMode.VersusCpu)
                modeText += " - CPU réfléchit...";

            ViewModeTextBlock.Text = modeText;
        }
    }

    private void ShowGameStatePopupIfNeeded()
    {
        if (HasAnyMoveForSideToMove())
            return;

        string sideToMoveText = _boardState.SideToMove == PieceColor.White
            ? "Blancs"
            : "Noirs";

        MessageBox.Show(
            $"Aucun coup disponible pour {sideToMoveText}.",
            "Fin de partie",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int dwAttribute,
        ref int pvAttribute,
        int cbAttribute);
}