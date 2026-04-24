using Chess3D.Core.Enums;

namespace Chess3D.Core.Models
{
    public sealed class GameState
    {
        public BoardState Board { get; } = new();
        public PieceColor SideToMove { get; set; } = PieceColor.White;
        public List<Move> MoveHistory { get; } = new();
        public string CurrentFen { get; set; } = "startpos";
    }
}
