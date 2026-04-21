using Chess3D.App.Wpf.Services;
using Chess3D.Core.Enums;
using Chess3D.Core.Models;
using Chess3D.Engine.Stockfish.Services;
using Chess3D.Rendering.Wpf.ViewModels;
using System;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Chess3D.App.Wpf.Views;

public partial class MainWindow : Window
{
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

    public MainWindow()
    {
        InitializeComponent();

        _boardState = BoardState.CreateInitial();
        _boardViewModel = new Board3DViewModel(_boardState);

        Viewport.Children.Add(new ModelVisual3D
        {
            Content = _boardViewModel.Scene
        });

        _cameraController = new OrbitCameraController(Viewport, Camera);
        _cameraController.SetTarget(new Point3D(0, 0.5, 0));
        _cameraController.SetDistance(13);
        _cameraController.SetAngles(0, -35);

        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await InitializeStockfishAsync();
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
            MessageBox.Show(
                $"Stockfish introuvable : {enginePath}",
                "Stockfish",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            _playVsCpu = false;
            return;
        }

        _stockfishClient = new StockfishUciClient(enginePath);
        await _stockfishClient.InitializeAsync();
        await _stockfishClient.NewGameAsync();
    }

    private void Root_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_isGameOver || _isCpuThinking)
            return;

        if (_playVsCpu && _boardState.SideToMove != _humanColor)
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
                    Title = $"Chess 3D - promotion vers {(char)('a' + file)}{rank + 1}";
            }

            if (!movePlayed)
            {
                _boardViewModel.ClearHighlights();
                Title = $"Chess 3D - case : {(char)('a' + file)}{rank + 1}";
                return;
            }
        }
        else
        {
            movePlayed = _boardViewModel.TryMoveSelectedPieceTo(file, rank);

            if (!movePlayed)
            {
                _boardViewModel.ClearHighlights();
                Title = $"Chess 3D - case : {(char)('a' + file)}{rank + 1}";
                return;
            }

            Title = $"Chess 3D - déplacement vers {(char)('a' + file)}{rank + 1}";
        }

        if (ShowGameStateIfNeeded())
            return;

        _ = TryPlayCpuMoveAsync();
    }

    private bool ShowGameStateIfNeeded()
    {
        var gameState = _boardState.GetGameEndState();

        switch (gameState)
        {
            case GameEndState.Check:
                Title = $"Chess 3D - échec sur les {(_boardState.SideToMove == PieceColor.White ? "blancs" : "noirs")}";
                return false;

            case GameEndState.Checkmate:
                {
                    _isGameOver = true;
                    string winner = _boardState.SideToMove == PieceColor.White ? "Noirs" : "Blancs";
                    Title = $"Chess 3D - échec et mat, victoire des {winner.ToLowerInvariant()}";
                    MessageBox.Show(
                    $"Échec et mat ! Victoire des {winner}.",
                    "Fin de partie",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                    return true;
                }

            case GameEndState.Stalemate:
                _isGameOver = true;
                Title = "Chess 3D - pat";
                MessageBox.Show(
                    "Pat ! La partie est nulle.",
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
        _cpuCts?.Cancel();
        _cpuCts?.Dispose();
        _cpuCts = new CancellationTokenSource();

        try
        {
            string fen = _boardState.ToFen();
            Move cpuMove = await _stockfishClient.GetBestMoveAsync(fen, _cpuLevel, _cpuCts.Token);

            _boardState.MakeMove(cpuMove);
            _boardViewModel.RefreshPiecesFromBoardState();
            Title = $"Chess 3D - CPU joue {cpuMove.ToUci()}";

            ShowGameStateIfNeeded();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Erreur CPU/Stockfish : {ex.Message}",
                "Erreur",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            _isCpuThinking = false;
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

        if (_stockfishClient is not null)
            await _stockfishClient.NewGameAsync();

        Title = "Chess 3D - nouvelle partie";
        await TryPlayCpuMoveAsync();
    }

    private void Quit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void LocalTwoPlayersMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _playVsCpu = false;
        await StartNewGameAsync();
    }

    private async void VersusCpuMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _playVsCpu = true;
        await InitializeStockfishAsync();
        await StartNewGameAsync();
    }

    private async void PlayWhiteMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _humanColor = PieceColor.White;
        await StartNewGameAsync();
    }

    private async void PlayBlackMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _humanColor = PieceColor.Black;
        await StartNewGameAsync();
    }

    private async void CpuLevel1MenuItem_Click(object sender, RoutedEventArgs e)
    {
        _cpuLevel = 1;
        await StartNewGameAsync();
    }

    private async void CpuLevel2MenuItem_Click(object sender, RoutedEventArgs e)
    {
        _cpuLevel = 2;
        await StartNewGameAsync();
    }

    private async void CpuLevel3MenuItem_Click(object sender, RoutedEventArgs e)
    {
        _cpuLevel = 3;
        await StartNewGameAsync();
    }

    private async void CpuLevel4MenuItem_Click(object sender, RoutedEventArgs e)
    {
        _cpuLevel = 4;
        await StartNewGameAsync();
    }

    private async void CpuLevel5MenuItem_Click(object sender, RoutedEventArgs e)
    {
        _cpuLevel = 5;
        await StartNewGameAsync();
    }

    private async void CpuLevel6MenuItem_Click(object sender, RoutedEventArgs e)
    {
        _cpuLevel = 6;
        await StartNewGameAsync();
    }

    private async void CpuLevel7MenuItem_Click(object sender, RoutedEventArgs e)
    {
        _cpuLevel = 7;
        await StartNewGameAsync();
    }

    private async void CpuLevel8MenuItem_Click(object sender, RoutedEventArgs e)
    {
        _cpuLevel = 8;
        await StartNewGameAsync();
    }

    private async void CpuLevel9MenuItem_Click(object sender, RoutedEventArgs e)
    {
        _cpuLevel = 9;
        await StartNewGameAsync();
    }

    private async void CpuLevel10MenuItem_Click(object sender, RoutedEventArgs e)
    {
        _cpuLevel = 10;
        await StartNewGameAsync();
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        string versionText = version is null
            ? "Version inconnue"
            : $"Version {version.Major}.{version.Minor}.{version.Build}";

        MessageBox.Show(
            "Chess 3D " + $"{versionText}" + " .NET / WPF" + "\nApplication d'échecs 3D avec moteur Stockfish." + "\nFonctionnalités : partie locale, partie contre CPU, affichage 3D, règles spéciales." + "\nAuteur : Etienne Jacquemoud", "A propos", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}