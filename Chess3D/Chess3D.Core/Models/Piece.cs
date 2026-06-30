using Chess3D.Core.Enums;

namespace Chess3D.Core.Models
{
    public sealed class Piece
    {
        public PieceType Type { get; }
        public PieceColor Color { get; }

        public Piece(PieceType type, PieceColor color)
        {
            Type = type;
            Color = color;
        }
    }
}
