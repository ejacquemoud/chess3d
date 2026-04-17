using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using Chess3D.App.Wpf.Services;
using Chess3D.Core.Enums;
using Chess3D.Core.Models;
using Chess3D.Rendering.Wpf.ViewModels;

namespace Chess3D.App.Wpf.Views;

public partial class MainWindow : Window
{
    private enum BoardViewMode
    {
        LocalTwoPlayers,
        VersusCpu
    }

    private readonly record struct CameraViewTarget(Point3D Position, Point3D Target, double FieldOfView);

    private readonly OrbitCameraController _cameraController;

    private BoardState _boardState = null!;
    private Board3DViewModel _boardViewModel = null!;
    private ModelVisual3D? _sceneVisual;

    private BoardViewMode _boardViewMode = BoardViewMode.LocalTwoPlayers;
    private PieceColor _humanPlayerColor = PieceColor.White;

    public MainWindow()
    {
        InitializeComponent();

        SourceInitialized += MainWindow_SourceInitialized;

        _cameraController = new OrbitCameraController(Viewport, Camera);

        StartNewGame();

        Root.MouseLeftButtonDown += Root_MouseLeftButtonDown;
    }

    private void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        EnableDarkTitleBar();
    }

    private void EnableDarkTitleBar()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero)
            return;

        int enabled = 1;

        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 18985))
        {
            _ = DwmSetWindowAttribute(
                hwnd,
                DWMWA_USE_IMMERSIVE_DARK_MODE,
                ref enabled,
                sizeof(int));
        }
        else if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
        {
            _ = DwmSetWindowAttribute(
                hwnd,
                DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1,
                ref enabled,
                sizeof(int));
        }
    }

    private void Quit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void Root_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
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
    }

    private void PlayWhiteMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _humanPlayerColor = PieceColor.White;

        PlayWhiteMenuItem.IsChecked = true;
        PlayBlackMenuItem.IsChecked = false;

        if (_boardViewMode == BoardViewMode.VersusCpu)
            AnimateCameraForCurrentMode();

        UpdateStatusDisplay();
    }

    private void PlayBlackMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _humanPlayerColor = PieceColor.Black;

        PlayWhiteMenuItem.IsChecked = false;
        PlayBlackMenuItem.IsChecked = true;

        if (_boardViewMode == BoardViewMode.VersusCpu)
            AnimateCameraForCurrentMode();

        UpdateStatusDisplay();
    }

    private void StartNewGame()
    {
        _boardState = BoardState.CreateInitial();
        _boardViewModel = new Board3DViewModel(_boardState);

        RenderScene();
        SetCameraInstantForCurrentMode();

        Title = "Chess 3D - nouvelle partie";
        UpdateStatusDisplay();
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

    private CameraViewTarget GetCameraViewTargetForCurrentMode()
    {
        Point3D target = new Point3D(0, 0.5, 0);
        var viewSide = GetViewSide();

        Point3D position;
        if (viewSide == PieceColor.White)
        {
            position = new Point3D(0, 7.5, -10.5);
        }
        else
        {
            position = new Point3D(0, 7.5, 10.5);
        }

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
    }

    private void UpdateStatusDisplay()
    {
        string sideToMoveText = _boardState.SideToMove == PieceColor.White
            ? "Blancs"
            : "Noirs";

        string statusText = _boardState.GetGameEndState() switch
        {
            GameEndState.None => $"Tour : {sideToMoveText}",
            GameEndState.Check => $"Tour : {sideToMoveText} - Échec",
            GameEndState.Checkmate => $"Tour : {sideToMoveText} - Échec et mat",
            GameEndState.Stalemate => $"Tour : {sideToMoveText} - Pat",
            _ => $"Tour : {sideToMoveText}"
        };

        if (StatusTextBlock != null)
            StatusTextBlock.Text = statusText;

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
            ViewModeTextBlock.Text = _boardViewMode switch
            {
                BoardViewMode.LocalTwoPlayers => "Mode : 2 joueurs",
                BoardViewMode.VersusCpu => _humanPlayerColor == PieceColor.White
                    ? "Mode : CPU - vous jouez les Blancs"
                    : "Mode : CPU - vous jouez les Noirs",
                _ => "Mode : inconnu"
            };
        }
    }

    private void ShowGameStatePopupIfNeeded()
    {
        string sideToMoveText = _boardState.SideToMove == PieceColor.White
            ? "Blancs"
            : "Noirs";

        switch (_boardState.GetGameEndState())
        {
            case GameEndState.Check:
                MessageBox.Show(
                    $"{sideToMoveText} sont en échec.",
                    "Échec",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                break;

            case GameEndState.Checkmate:
                MessageBox.Show(
                    $"{sideToMoveText} sont en échec et mat.",
                    "Échec et mat",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                break;

            case GameEndState.Stalemate:
                MessageBox.Show(
                    "La partie est nulle par pat.",
                    "Pat",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                break;
        }
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