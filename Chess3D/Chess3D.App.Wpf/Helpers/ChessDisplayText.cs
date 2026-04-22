using Chess3D.Rendering.Wpf.Enums;

namespace Chess3D.App.Wpf.Helpers;

public static class ChessDisplayText
{
    public static string ToFrench(PieceType pieceType) => pieceType switch
    {
        PieceType.Pawn => "Pion",
        PieceType.Knight => "Cavalier",
        PieceType.Bishop => "Fou",
        PieceType.Rook => "Tour",
        PieceType.Queen => "Dame",
        PieceType.King => "Roi",
        _ => "Inconnue"
    };

    public static string ToFrench(PieceColor pieceColor) => pieceColor switch
    {
        PieceColor.White => "blanc",
        PieceColor.Black => "noir",
        _ => "inconnue"
    };
}