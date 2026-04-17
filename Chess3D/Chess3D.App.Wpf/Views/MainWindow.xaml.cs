using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Chess3D.App.Wpf.Services;
using Chess3D.Core.Models;
using Chess3D.Rendering.Wpf.ViewModels;

namespace Chess3D.App.Wpf.Views;

public partial class MainWindow : Window
{
    private readonly OrbitCameraController _cameraController;
    private readonly ModelVisual3D _boardVisual = new();

    private BoardState _boardState;
    private Board3DViewModel _boardViewModel;

    public MainWindow()
    {
        InitializeComponent();

        _boardState = BoardState.CreateInitial();
        _boardViewModel = new Board3DViewModel(_boardState);

        _boardVisual.Content = _boardViewModel.Scene;
        Viewport.Children.Add(_boardVisual);

        _cameraController = new OrbitCameraController(Viewport, Camera);
        _cameraController.SetTarget(new Point3D(0, 0.5, 0));
        _cameraController.SetDistance(13);
        _cameraController.SetAngles(0, -35);

        Title = "Chess 3D";
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
                    return;
                }
            }

            _boardViewModel.ClearHighlights();
            Title = $"Chess 3D - case : {(char)('a' + file)}{rank + 1}";
            return;
        }

        if (_boardViewModel.TryMoveSelectedPieceTo(file, rank))
        {
            Title = $"Chess 3D - déplacement vers {(char)('a' + file)}{rank + 1}";
        }
        else
        {
            _boardViewModel.ClearHighlights();
            Title = $"Chess 3D - case : {(char)('a' + file)}{rank + 1}";
        }
    }

    private void NewGame_Click(object sender, RoutedEventArgs e)
    {
        StartNewGame();
    }

    private void StartNewGame()
    {
        _boardState = BoardState.CreateInitial();
        _boardViewModel = new Board3DViewModel(_boardState);
        _boardVisual.Content = _boardViewModel.Scene;

        Camera.Position = new Point3D(0, 10, 13);
        Camera.LookDirection = new Vector3D(0, -8, -13);
        Camera.UpDirection = new Vector3D(0, 1, 0);
        Camera.FieldOfView = 60;

        _cameraController.SetTarget(new Point3D(0, 0.5, 0));
        _cameraController.SetDistance(13);
        _cameraController.SetAngles(0, -35);

        Title = "Chess 3D - nouvelle partie";
    }
}