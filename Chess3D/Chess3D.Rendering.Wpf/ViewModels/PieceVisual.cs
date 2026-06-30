using System.Collections.Generic;
using System.Windows.Media.Media3D;
using Chess3D.Rendering.Wpf.Enums;

namespace Chess3D.Rendering.Wpf.ViewModels;

public sealed class PieceVisual
{
    public PieceType PieceType { get; set; }
    public PieceColor PieceColor { get; set; }
    public int File { get; set; }
    public int Rank { get; set; }

    public List<Model3D> Models { get; } = new();

    public bool IsAnimating { get; set; }
}