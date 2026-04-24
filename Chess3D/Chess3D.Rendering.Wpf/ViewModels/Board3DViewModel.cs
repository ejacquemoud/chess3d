using CorePieceType = Chess3D.Core.Enums.PieceType;
using CorePieceColor = Chess3D.Core.Enums.PieceColor;
using Chess3D.Core.Models;
using Chess3D.Rendering.Wpf.Geometry;
using WpfPieceType = Chess3D.Rendering.Wpf.Enums.PieceType;
using WpfPieceColor = Chess3D.Rendering.Wpf.Enums.PieceColor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;

namespace Chess3D.Rendering.Wpf.ViewModels;

public sealed class Board3DViewModel
{
    public Model3DGroup Scene { get; }

    private BoardState _boardState;
    private readonly Dictionary<Model3D, PieceVisual> _modelToPiece = new();
    private readonly List<GeometryModel3D> _moveHighlights = new();
    private readonly List<PieceVisual> _pieces = new();

    private GeometryModel3D? _selectionHighlight;
    private bool _isAnimatingMove;

    public PieceVisual? SelectedPiece { get; private set; }
    public List<(int file, int rank)> CurrentMoves { get; } = new();

    private const double TileSize = 1.0;
    private const double TileHeight = 0.12;
    private const double BoardTopY = 0.0;

    public Board3DViewModel(BoardState boardState)
    {
        _boardState = boardState;
        Scene = BuildScene();
        RefreshPiecesFromBoardState();
    }

    private Model3DGroup BuildScene()
    {
        var group = new Model3DGroup();

        group.Children.Add(new AmbientLight(Color.FromRgb(95, 95, 95)));
        group.Children.Add(new DirectionalLight(Color.FromRgb(255, 244, 228), new Vector3D(-1.2, -2.4, -1.0)));
        group.Children.Add(new DirectionalLight(Color.FromRgb(110, 125, 150), new Vector3D(1.0, -0.8, 1.4)));
        group.Children.Add(new DirectionalLight(Color.FromRgb(70, 70, 80), new Vector3D(0.2, -1.0, -1.8)));

        var darkSquare = Color.FromRgb(181, 136, 99);
        var lightSquare = Color.FromRgb(240, 217, 181);
        var boardBase = Color.FromRgb(70, 45, 30);

        group.Children.Add(CreateBox(new Point3D(0, -0.25, 0), 8.8, 0.5, 8.8, boardBase));

        for (int rank = 0; rank < 8; rank++)
        {
            for (int file = 0; file < 8; file++)
            {
                double x = (file - 3.5) * TileSize;
                double z = (rank - 3.5) * TileSize;
                bool isLight = ((file + rank) % 2 == 0);

                group.Children.Add(CreateBox(
                    new Point3D(x, BoardTopY + TileHeight / 2.0, z),
                    TileSize,
                    TileHeight,
                    TileSize,
                    isLight ? lightSquare : darkSquare));
            }
        }

        return group;
    }

    public void RefreshPiecesFromBoardState()
    {
        ClearPieceVisuals();

        foreach (var entry in _boardState.GetOccupiedSquares())
        {
            AddPieceVisual(
                MapPieceType(entry.Piece.Type),
                MapPieceColor(entry.Piece.Color),
                entry.Square.File,
                entry.Square.Rank);
        }

        ClearHighlights();
        _isAnimatingMove = false;
    }

    private void ClearPieceVisuals()
    {
        foreach (var piece in _pieces)
        {
            foreach (var model in piece.Models)
                Scene.Children.Remove(model);
        }

        _pieces.Clear();
        _modelToPiece.Clear();
        SelectedPiece = null;
    }

    private void AddPieceVisual(WpfPieceType type, WpfPieceColor color, int file, int rank)
    {
        var piece = new PieceVisual
        {
            PieceType = type,
            PieceColor = color,
            File = file,
            Rank = rank
        };

        var baseCenter = GetSquareCenter(file, rank);
        var models = ChessPieceFactory.CreatePieceModels(type, color, baseCenter);

        foreach (var model in models)
        {
            piece.Models.Add(model);
            _modelToPiece[model] = piece;
            Scene.Children.Add(model);
        }

        _pieces.Add(piece);
    }

    public bool TrySelectPiece(Model3D model)
    {
        if (_isAnimatingMove)
            return false;

        if (!_modelToPiece.TryGetValue(model, out var piece))
            return false;

        return TrySelectPieceInternal(piece);
    }

    public bool TrySelectPieceAt(int file, int rank)
    {
        if (_isAnimatingMove)
            return false;

        var piece = _pieces.FirstOrDefault(p => p.File == file && p.Rank == rank);
        if (piece == null)
            return false;

        return TrySelectPieceInternal(piece);
    }

    private bool TrySelectPieceInternal(PieceVisual piece)
    {
        var from = new Square(piece.File, piece.Rank);
        var moves = _boardState.GenerateLegalMovesFor(from);

        if (moves.Count == 0)
            return false;

        SelectedPiece = piece;
        HighlightSquare(piece.File, piece.Rank);
        ShowMoves(moves.Select(m => (m.To.File, m.To.Rank)));
        return true;
    }

    public bool TryGetSquareFromHit(Point3D hitPoint, out int file, out int rank)
    {
        file = (int)Math.Floor(hitPoint.X + 4.0);
        rank = (int)Math.Floor(hitPoint.Z + 4.0);

        if (file < 0 || file > 7 || rank < 0 || rank > 7)
            return false;

        return true;
    }

    public void ShowMoves(IEnumerable<(int file, int rank)> moves)
    {
        ClearMoveHighlights();
        CurrentMoves.Clear();

        foreach (var move in moves)
        {
            CurrentMoves.Add(move);

            var center = GetSquareCenter(move.file, move.rank);
            var highlight = CreateBox(
                new Point3D(center.X, BoardTopY + TileHeight + 0.015, center.Z),
                0.55,
                0.02,
                0.55,
                Color.FromArgb(180, 80, 255, 120));

            _moveHighlights.Add(highlight);
            Scene.Children.Add(highlight);
        }
    }

    public void HighlightSquare(int file, int rank)
    {
        if (_selectionHighlight != null)
        {
            Scene.Children.Remove(_selectionHighlight);
            _selectionHighlight = null;
        }

        var center = GetSquareCenter(file, rank);

        _selectionHighlight = CreateBox(
            new Point3D(center.X, BoardTopY + TileHeight + 0.02, center.Z),
            0.92,
            0.03,
            0.92,
            Color.FromArgb(180, 80, 180, 255));

        Scene.Children.Add(_selectionHighlight);
    }

    public void ClearHighlights()
    {
        if (_selectionHighlight != null)
        {
            Scene.Children.Remove(_selectionHighlight);
            _selectionHighlight = null;
        }

        ClearMoveHighlights();
        CurrentMoves.Clear();
    }

    private void ClearMoveHighlights()
    {
        foreach (var h in _moveHighlights)
            Scene.Children.Remove(h);

        _moveHighlights.Clear();
    }

    private Point3D GetSquareCenter(int file, int rank)
    {
        double x = file - 3.5;
        double z = rank - 3.5;
        double y = BoardTopY + TileHeight;
        return new Point3D(x, y, z);
    }

    private PieceVisual? GetPieceVisualAt(int file, int rank, PieceVisual? except = null)
    {
        return _pieces.FirstOrDefault(p =>
            p != except &&
            p.File == file &&
            p.Rank == rank);
    }

    private PieceVisual? GetCapturedPieceVisualForMove(PieceVisual movingPiece, Square from, Square to)
    {
        var directCapture = GetPieceVisualAt(to.File, to.Rank, movingPiece);
        if (directCapture != null)
            return directCapture;

        var movingBoardPiece = _boardState.GetPiece(from);
        if (movingBoardPiece == null || movingBoardPiece.Type != CorePieceType.Pawn)
            return null;

        if (!_boardState.EnPassantTargetSquare.HasValue || _boardState.EnPassantTargetSquare.Value != to)
            return null;

        if (from.File == to.File)
            return null;

        return GetPieceVisualAt(to.File, from.Rank, movingPiece);
    }

    private PieceVisual? GetRookVisualForCastlingBeforeMove(PieceVisual movingPiece, Square from, Square to)
    {
        var movingBoardPiece = _boardState.GetPiece(from);
        if (movingBoardPiece == null || movingBoardPiece.Type != CorePieceType.King)
            return null;

        if (Math.Abs(to.File - from.File) != 2)
            return null;

        int rookFromFile = to.File > from.File ? 7 : 0;
        return GetPieceVisualAt(rookFromFile, from.Rank, movingPiece);
    }

    private void RemovePieceVisual(PieceVisual piece)
    {
        foreach (var model in piece.Models)
        {
            Scene.Children.Remove(model);
            _modelToPiece.Remove(model);
        }

        _pieces.Remove(piece);

        if (ReferenceEquals(SelectedPiece, piece))
            SelectedPiece = null;
    }

    private void ReplacePieceVisual(PieceVisual oldPiece, WpfPieceType newType, WpfPieceColor newColor, int file, int rank)
    {
        RemovePieceVisual(oldPiece);
        AddPieceVisual(newType, newColor, file, rank);
    }

    private void ResetPieceTransforms(PieceVisual piece)
    {
        foreach (var model in piece.Models)
        {
            var translate = EnsureTranslateTransform(model);
            translate.BeginAnimation(TranslateTransform3D.OffsetXProperty, null);
            translate.BeginAnimation(TranslateTransform3D.OffsetYProperty, null);
            translate.BeginAnimation(TranslateTransform3D.OffsetZProperty, null);
            translate.OffsetX = 0;
            translate.OffsetY = 0;
            translate.OffsetZ = 0;
        }
    }

    private void RebasePieceToSquare(PieceVisual piece, int newFile, int newRank)
    {
        ResetPieceTransforms(piece);

        foreach (var model in piece.Models)
        {
            if (model is not GeometryModel3D geometryModel)
                continue;

            Point3D newCenter = GetSquareCenter(newFile, newRank);
            var material = geometryModel.Material;
            var backMaterial = geometryModel.BackMaterial;

            Scene.Children.Remove(geometryModel);
            _modelToPiece.Remove(geometryModel);

            var recreatedModels = ChessPieceFactory.CreatePieceModels(piece.PieceType, piece.PieceColor, newCenter);
            foreach (var recreated in recreatedModels)
            {
                piece.Models.Add(recreated);
                _modelToPiece[recreated] = piece;
                Scene.Children.Add(recreated);
            }

            break;
        }

        foreach (var oldModel in piece.Models.ToList())
        {
            if (!_modelToPiece.ContainsKey(oldModel))
                piece.Models.Remove(oldModel);
        }

        piece.File = newFile;
        piece.Rank = newRank;
    }

    private TranslateTransform3D EnsureTranslateTransform(Model3D model)
    {
        if (model.Transform is TranslateTransform3D translate)
            return translate;

        if (model.Transform is Transform3DGroup group)
        {
            foreach (var child in group.Children)
            {
                if (child is TranslateTransform3D childTranslate)
                    return childTranslate;
            }

            var newTranslate = new TranslateTransform3D();
            group.Children.Add(newTranslate);
            return newTranslate;
        }

        if (model.Transform == null || model.Transform == Transform3D.Identity)
        {
            var newTranslate = new TranslateTransform3D();
            model.Transform = newTranslate;
            return newTranslate;
        }

        var transformGroup = new Transform3DGroup();
        transformGroup.Children.Add(model.Transform);
        var translateTransform = new TranslateTransform3D();
        transformGroup.Children.Add(translateTransform);
        model.Transform = transformGroup;
        return translateTransform;
    }

    private static SolidColorBrush CreateAnimatedBrushFromMaterial(Material? material, Color fallbackColor)
    {
        if (material is DiffuseMaterial dm && dm.Brush is SolidColorBrush existing)
        {
            var brush = existing.IsFrozen ? existing.Clone() : existing.Clone();
            brush.Opacity = 1.0;
            return brush;
        }

        return new SolidColorBrush(fallbackColor);
    }

    private void FadeOutPiece(PieceVisual piece, int durationMs, Action? completed = null)
    {
        if (piece.IsAnimating)
        {
            completed?.Invoke();
            return;
        }

        piece.IsAnimating = true;

        var duration = TimeSpan.FromMilliseconds(durationMs);
        var geometryModels = piece.Models.OfType<GeometryModel3D>().ToList();

        if (geometryModels.Count == 0)
        {
            RemovePieceVisual(piece);
            piece.IsAnimating = false;
            completed?.Invoke();
            return;
        }

        int completedCount = 0;

        void HandleCompleted()
        {
            completedCount++;
            if (completedCount < geometryModels.Count)
                return;

            RemovePieceVisual(piece);
            piece.IsAnimating = false;
            completed?.Invoke();
        }

        Color fallbackColor = piece.PieceColor == WpfPieceColor.White
            ? Color.FromRgb(245, 245, 245)
            : Color.FromRgb(35, 35, 35);

        foreach (var model in geometryModels)
        {
            var frontBrush = CreateAnimatedBrushFromMaterial(model.Material, fallbackColor);
            var backBrush = CreateAnimatedBrushFromMaterial(model.BackMaterial, fallbackColor);

            model.Material = new DiffuseMaterial(frontBrush);
            model.BackMaterial = new DiffuseMaterial(backBrush);

            var fadeFront = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = duration,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            var fadeBack = fadeFront.Clone();
            fadeFront.Completed += (_, _) => HandleCompleted();

            frontBrush.BeginAnimation(SolidColorBrush.OpacityProperty, fadeFront);
            backBrush.BeginAnimation(SolidColorBrush.OpacityProperty, fadeBack);
        }
    }

    private void AnimatePieceToSquare(PieceVisual piece, int fromFile, int fromRank, int toFile, int toRank, Action? completed)
    {
        if (piece.IsAnimating)
            return;

        piece.IsAnimating = true;

        var from = GetSquareCenter(fromFile, fromRank);
        var to = GetSquareCenter(toFile, toRank);

        double deltaX = to.X - from.X;
        double deltaZ = to.Z - from.Z;

        var translates = piece.Models.Select(EnsureTranslateTransform).ToList();

        foreach (var translate in translates)
        {
            translate.BeginAnimation(TranslateTransform3D.OffsetXProperty, null);
            translate.BeginAnimation(TranslateTransform3D.OffsetYProperty, null);
            translate.BeginAnimation(TranslateTransform3D.OffsetZProperty, null);

            translate.OffsetX = 0;
            translate.OffsetY = 0;
            translate.OffsetZ = 0;
        }

        var duration = TimeSpan.FromMilliseconds(300);
        int completedCount = 0;

        void HandleCompleted()
        {
            completedCount++;
            if (completedCount < translates.Count)
                return;

            foreach (var translate in translates)
            {
                translate.BeginAnimation(TranslateTransform3D.OffsetXProperty, null);
                translate.BeginAnimation(TranslateTransform3D.OffsetYProperty, null);
                translate.BeginAnimation(TranslateTransform3D.OffsetZProperty, null);
                translate.OffsetX = deltaX;
                translate.OffsetY = 0;
                translate.OffsetZ = deltaZ;
            }

            piece.IsAnimating = false;
            completed?.Invoke();
        }

        foreach (var translate in translates)
        {
            var animX = new DoubleAnimation
            {
                From = 0,
                To = deltaX,
                Duration = duration,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            var animZ = new DoubleAnimation
            {
                From = 0,
                To = deltaZ,
                Duration = duration,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            var animY = new DoubleAnimationUsingKeyFrames
            {
                Duration = duration
            };

            animY.KeyFrames.Add(new EasingDoubleKeyFrame(0.0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            animY.KeyFrames.Add(new EasingDoubleKeyFrame(0.10, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(120))));
            animY.KeyFrames.Add(new EasingDoubleKeyFrame(0.0, KeyTime.FromTimeSpan(duration)));

            animZ.Completed += (_, _) => HandleCompleted();

            translate.BeginAnimation(TranslateTransform3D.OffsetXProperty, animX);
            translate.BeginAnimation(TranslateTransform3D.OffsetYProperty, animY);
            translate.BeginAnimation(TranslateTransform3D.OffsetZProperty, animZ);
        }
    }

    private void ApplyPostMoveVisualState(PieceVisual movingPiece, Move move)
    {
        var from = move.From;
        var to = move.To;

        if (move.Promotion != CorePieceType.None)
        {
            var promotedType = MapPieceType(move.Promotion);
            var promotedColor = movingPiece.PieceColor;
            ReplacePieceVisual(movingPiece, promotedType, promotedColor, to.File, to.Rank);
        }
        else
        {
            RebasePieceToSquare(movingPiece, to.File, to.Rank);
        }

        var movedBoardPiece = _boardState.GetPiece(to);
        if (movedBoardPiece != null &&
            movedBoardPiece.Type == CorePieceType.King &&
            Math.Abs(to.File - from.File) == 2)
        {
            int rookFromFile = to.File > from.File ? 7 : 0;
            int rookToFile = to.File > from.File ? 5 : 3;

            var rookVisual = GetPieceVisualAt(rookFromFile, from.Rank);
            if (rookVisual != null)
                RebasePieceToSquare(rookVisual, rookToFile, from.Rank);
        }
    }

    public bool TryMoveSelectedPieceTo(int file, int rank)
    {
        if (SelectedPiece == null || _isAnimatingMove)
            return false;

        var from = new Square(SelectedPiece.File, SelectedPiece.Rank);
        var to = new Square(file, rank);
        var moves = _boardState.GenerateLegalMovesFor(from);

        var matchingMoves = moves.Where(m => m.To.File == file && m.To.Rank == rank).ToList();
        if (matchingMoves.Count == 0)
            return false;

        var selectedMove = matchingMoves.FirstOrDefault(m => m.Promotion == CorePieceType.Queen) ?? matchingMoves[0];
        var piece = SelectedPiece;
        var capturedPiece = GetCapturedPieceVisualForMove(piece, from, to);

        ClearHighlights();

        if (capturedPiece != null)
            RemovePieceVisual(capturedPiece);

        _boardState.MakeMove(selectedMove);
        ApplyPostMoveVisualState(piece, selectedMove);

        SelectedPiece = null;
        return true;
    }

    public bool TryMoveSelectedPieceTo(int file, int rank, CorePieceType promotion)
    {
        if (SelectedPiece == null || _isAnimatingMove)
            return false;

        var from = new Square(SelectedPiece.File, SelectedPiece.Rank);
        var to = new Square(file, rank);
        var moves = _boardState.GenerateLegalMovesFor(from);

        var selectedMove = moves.FirstOrDefault(m =>
            m.To.File == file &&
            m.To.Rank == rank &&
            m.Promotion == promotion);

        if (selectedMove == null)
            return false;

        var piece = SelectedPiece;
        var capturedPiece = GetCapturedPieceVisualForMove(piece, from, to);

        ClearHighlights();

        if (capturedPiece != null)
            RemovePieceVisual(capturedPiece);

        _boardState.MakeMove(selectedMove);
        ApplyPostMoveVisualState(piece, selectedMove);

        SelectedPiece = null;
        return true;
    }

    public bool TryAnimateMoveSelectedPieceTo(int file, int rank, Action? onCompleted)
    {
        if (SelectedPiece == null || _isAnimatingMove)
            return false;

        var from = new Square(SelectedPiece.File, SelectedPiece.Rank);
        var to = new Square(file, rank);
        var moves = _boardState.GenerateLegalMovesFor(from);

        var selectedMove = moves.FirstOrDefault(m =>
            m.To.File == file &&
            m.To.Rank == rank &&
            m.Promotion == CorePieceType.None);

        if (selectedMove == null)
            return false;

        var piece = SelectedPiece;
        var capturedPiece = GetCapturedPieceVisualForMove(piece, from, to);

        ClearHighlights();
        _isAnimatingMove = true;

        void StartMoveAnimation()
        {
            AnimatePieceToSquare(piece, from.File, from.Rank, file, rank, () =>
            {
                _boardState.MakeMove(selectedMove);
                ApplyPostMoveVisualState(piece, selectedMove);
                SelectedPiece = null;
                _isAnimatingMove = false;
                onCompleted?.Invoke();
            });
        }

        if (capturedPiece != null)
        {
            FadeOutPiece(capturedPiece, 120, StartMoveAnimation);
        }
        else
        {
            StartMoveAnimation();
        }

        return true;
    }

    public bool TryAnimateMoveSelectedPieceTo(int file, int rank, CorePieceType promotion, Action? onCompleted)
    {
        if (SelectedPiece == null || _isAnimatingMove)
            return false;

        var from = new Square(SelectedPiece.File, SelectedPiece.Rank);
        var to = new Square(file, rank);
        var moves = _boardState.GenerateLegalMovesFor(from);

        var selectedMove = moves.FirstOrDefault(m =>
            m.To.File == file &&
            m.To.Rank == rank &&
            m.Promotion == promotion);

        if (selectedMove == null)
            return false;

        var piece = SelectedPiece;
        var capturedPiece = GetCapturedPieceVisualForMove(piece, from, to);

        ClearHighlights();
        _isAnimatingMove = true;

        void StartMoveAnimation()
        {
            AnimatePieceToSquare(piece, from.File, from.Rank, file, rank, () =>
            {
                _boardState.MakeMove(selectedMove);
                ApplyPostMoveVisualState(piece, selectedMove);
                SelectedPiece = null;
                _isAnimatingMove = false;
                onCompleted?.Invoke();
            });
        }

        if (capturedPiece != null)
        {
            FadeOutPiece(capturedPiece, 120, StartMoveAnimation);
        }
        else
        {
            StartMoveAnimation();
        }

        return true;
    }

    public bool RequiresPromotion(int file, int rank)
    {
        if (SelectedPiece == null || _isAnimatingMove)
            return false;

        var from = new Square(SelectedPiece.File, SelectedPiece.Rank);
        var moves = _boardState.GenerateLegalMovesFor(from);

        return moves.Any(m =>
            m.To.File == file &&
            m.To.Rank == rank &&
            m.Promotion != CorePieceType.None);
    }

    private static WpfPieceType MapPieceType(CorePieceType type) => type switch
    {
        CorePieceType.Pawn => WpfPieceType.Pawn,
        CorePieceType.Rook => WpfPieceType.Rook,
        CorePieceType.Knight => WpfPieceType.Knight,
        CorePieceType.Bishop => WpfPieceType.Bishop,
        CorePieceType.Queen => WpfPieceType.Queen,
        CorePieceType.King => WpfPieceType.King,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    private static WpfPieceColor MapPieceColor(CorePieceColor color) => color switch
    {
        CorePieceColor.White => WpfPieceColor.White,
        CorePieceColor.Black => WpfPieceColor.Black,
        _ => throw new ArgumentOutOfRangeException(nameof(color), color, null)
    };

    private static GeometryModel3D CreateBox(Point3D center, double sizeX, double sizeY, double sizeZ, Color color)
    {
        double hx = sizeX / 2.0;
        double hy = sizeY / 2.0;
        double hz = sizeZ / 2.0;

        var p0 = new Point3D(center.X - hx, center.Y - hy, center.Z - hz);
        var p1 = new Point3D(center.X + hx, center.Y - hy, center.Z - hz);
        var p2 = new Point3D(center.X + hx, center.Y + hy, center.Z - hz);
        var p3 = new Point3D(center.X - hx, center.Y + hy, center.Z - hz);
        var p4 = new Point3D(center.X - hx, center.Y - hy, center.Z + hz);
        var p5 = new Point3D(center.X + hx, center.Y - hy, center.Z + hz);
        var p6 = new Point3D(center.X + hx, center.Y + hy, center.Z + hz);
        var p7 = new Point3D(center.X - hx, center.Y + hy, center.Z + hz);

        var mesh = new MeshGeometry3D
        {
            Positions = new Point3DCollection { p0, p1, p2, p3, p4, p5, p6, p7 },
            TriangleIndices = new Int32Collection
            {
                4,5,6, 4,6,7,
                0,2,1, 0,3,2,
                0,4,7, 0,7,3,
                1,2,6, 1,6,5,
                3,7,6, 3,6,2,
                0,1,5, 0,5,4
            }
        };

        var material = new DiffuseMaterial(new SolidColorBrush(color));

        return new GeometryModel3D
        {
            Geometry = mesh,
            Material = material,
            BackMaterial = material
        };
    }

    public void ResetBoard(BoardState newState)
    {
        if (newState == null)
            throw new ArgumentNullException(nameof(newState));

        _isAnimatingMove = false;

        ClearHighlights();
        SelectedPiece = null;

        _boardState = newState;

        RefreshPiecesFromBoardState();
    }
}