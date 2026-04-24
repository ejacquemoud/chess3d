using Chess3D.Core.Enums;

namespace Chess3D.Core.Models
{
    public sealed class Piece
    {
        public PieceType Type { get; set; }
        public PieceColor Color { get; set; }

        public Piece(PieceType type, PieceColor color)
        {
            Type = type;
            Color = color;
        }
    }
}
