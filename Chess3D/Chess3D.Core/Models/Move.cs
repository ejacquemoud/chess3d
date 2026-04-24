using Chess3D.Core.Enums;

namespace Chess3D.Core.Models
{
    public sealed class Move
    {
        public Square From { get; init; }
        public Square To { get; init; }
        public PieceType Promotion { get; init; } = PieceType.None;

        public string ToUci() => $"{From}{To}{PromotionSuffix()}";

        private string PromotionSuffix() => Promotion switch
        {
            PieceType.Queen => "q",
            PieceType.Rook => "r",
            PieceType.Bishop => "b",
            PieceType.Knight => "n",
            _ => string.Empty
        };

        public static Move FromUci(string uci)
        {
            var from = Square.FromAlgebraic(uci[..2]);
            var to = Square.FromAlgebraic(uci[2..4]);

            var promotion = uci.Length > 4 ? uci[4] switch
            {
                'q' => PieceType.Queen,
                'r' => PieceType.Rook,
                'b' => PieceType.Bishop,
                'n' => PieceType.Knight,
                _ => PieceType.None
            } : PieceType.None;

            return new Move { From = from, To = to, Promotion = promotion };
        }
    }
}
