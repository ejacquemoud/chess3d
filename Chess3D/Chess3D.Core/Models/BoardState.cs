using System.Collections.Generic;
using Chess3D.Core.Enums;

namespace Chess3D.Core.Models
{
    public sealed class BoardState
    {
        private readonly Piece?[,] _board = new Piece?[8, 8];

        public PieceColor SideToMove { get; private set; } = PieceColor.White;

        public Piece? GetPiece(Square square) => _board[square.File, square.Rank];

        public void SetPiece(Square square, Piece? piece) => _board[square.File, square.Rank] = piece;

        public Piece?[,] Snapshot()
        {
            var copy = new Piece?[8, 8];
            System.Array.Copy(_board, copy, _board.Length);
            return copy;
        }

        public IEnumerable<(Square Square, Piece Piece)> GetOccupiedSquares()
        {
            for (int file = 0; file < 8; file++)
            {
                for (int rank = 0; rank < 8; rank++)
                {
                    var piece = _board[file, rank];
                    if (piece != null)
                        yield return (new Square(file, rank), piece);
                }
            }
        }

        public IReadOnlyList<Move> GeneratePseudoLegalMovesFor(Square from)
        {
            var moves = new List<Move>();
            var piece = GetPiece(from);

            if (piece == null)
                return moves;

            if (piece.Color != SideToMove)
                return moves;

            switch (piece.Type)
            {
                case PieceType.Pawn:
                    AddPawnMoves(moves, from, piece);
                    break;

                case PieceType.Rook:
                    AddRookMoves(moves, from, piece.Color);
                    break;

                case PieceType.Knight:
                    AddKnightMoves(moves, from, piece.Color);
                    break;

                case PieceType.Bishop:
                    AddBishopMoves(moves, from, piece.Color);
                    break;

                case PieceType.Queen:
                    AddQueenMoves(moves, from, piece.Color);
                    break;

                case PieceType.King:
                    AddKingMoves(moves, from, piece.Color);
                    break;
            }

            return moves;
        }

        public void MakeMove(Move move)
        {
            var piece = GetPiece(move.From);
            if (piece == null)
                return;

            if (piece.Type == PieceType.Pawn)
            {
                bool reachesLastRank =
                    (piece.Color == PieceColor.White && move.To.Rank == 7) ||
                    (piece.Color == PieceColor.Black && move.To.Rank == 0);

                if (reachesLastRank && move.Promotion != PieceType.None)
                {
                    SetPiece(move.To, new Piece(move.Promotion, piece.Color));
                    SetPiece(move.From, null);
                    ToggleSideToMove();
                    return;
                }
            }

            SetPiece(move.To, piece);
            SetPiece(move.From, null);
            ToggleSideToMove();
        }

        private void ToggleSideToMove()
        {
            SideToMove = SideToMove == PieceColor.White
                ? PieceColor.Black
                : PieceColor.White;
        }

        private void AddPawnMoves(List<Move> moves, Square from, Piece piece)
        {
            int direction = piece.Color == PieceColor.White ? 1 : -1;
            int startRank = piece.Color == PieceColor.White ? 1 : 6;

            var oneForward = new Square(from.File, from.Rank + direction);
            if (IsInside(oneForward) && GetPiece(oneForward) == null)
            {
                AddPawnAdvanceMove(moves, from, oneForward, piece.Color);

                var twoForward = new Square(from.File, from.Rank + 2 * direction);
                if (from.Rank == startRank && IsInside(twoForward) && GetPiece(twoForward) == null)
                {
                    moves.Add(new Move
                    {
                        From = from,
                        To = twoForward
                    });
                }
            }

            var captureLeft = new Square(from.File - 1, from.Rank + direction);
            var captureRight = new Square(from.File + 1, from.Rank + direction);

            AddPawnCaptureIfEnemy(moves, from, captureLeft, piece.Color);
            AddPawnCaptureIfEnemy(moves, from, captureRight, piece.Color);
        }

        private void AddPawnAdvanceMove(List<Move> moves, Square from, Square to, PieceColor color)
        {
            bool reachesLastRank =
                (color == PieceColor.White && to.Rank == 7) ||
                (color == PieceColor.Black && to.Rank == 0);

            if (reachesLastRank)
            {
                moves.Add(new Move { From = from, To = to, Promotion = PieceType.Queen });
                moves.Add(new Move { From = from, To = to, Promotion = PieceType.Rook });
                moves.Add(new Move { From = from, To = to, Promotion = PieceType.Bishop });
                moves.Add(new Move { From = from, To = to, Promotion = PieceType.Knight });
            }
            else
            {
                moves.Add(new Move { From = from, To = to });
            }
        }

        private void AddPawnCaptureIfEnemy(List<Move> moves, Square from, Square to, PieceColor ownColor)
        {
            if (!IsInside(to))
                return;

            var target = GetPiece(to);
            if (target == null || target.Color == ownColor)
                return;

            bool reachesLastRank =
                (ownColor == PieceColor.White && to.Rank == 7) ||
                (ownColor == PieceColor.Black && to.Rank == 0);

            if (reachesLastRank)
            {
                moves.Add(new Move { From = from, To = to, Promotion = PieceType.Queen });
                moves.Add(new Move { From = from, To = to, Promotion = PieceType.Rook });
                moves.Add(new Move { From = from, To = to, Promotion = PieceType.Bishop });
                moves.Add(new Move { From = from, To = to, Promotion = PieceType.Knight });
            }
            else
            {
                moves.Add(new Move { From = from, To = to });
            }
        }

        private void AddRookMoves(List<Move> moves, Square from, PieceColor ownColor)
        {
            AddRayMoves(moves, from, ownColor, 1, 0);
            AddRayMoves(moves, from, ownColor, -1, 0);
            AddRayMoves(moves, from, ownColor, 0, 1);
            AddRayMoves(moves, from, ownColor, 0, -1);
        }

        private void AddBishopMoves(List<Move> moves, Square from, PieceColor ownColor)
        {
            AddRayMoves(moves, from, ownColor, 1, 1);
            AddRayMoves(moves, from, ownColor, 1, -1);
            AddRayMoves(moves, from, ownColor, -1, 1);
            AddRayMoves(moves, from, ownColor, -1, -1);
        }

        private void AddQueenMoves(List<Move> moves, Square from, PieceColor ownColor)
        {
            AddRookMoves(moves, from, ownColor);
            AddBishopMoves(moves, from, ownColor);
        }

        private void AddKnightMoves(List<Move> moves, Square from, PieceColor ownColor)
        {
            var offsets = new (int df, int dr)[]
            {
                ( 1,  2),
                ( 2,  1),
                ( 2, -1),
                ( 1, -2),
                (-1, -2),
                (-2, -1),
                (-2,  1),
                (-1,  2)
            };

            foreach (var (df, dr) in offsets)
            {
                var to = new Square(from.File + df, from.Rank + dr);
                if (!IsInside(to))
                    continue;

                var target = GetPiece(to);
                if (target == null || target.Color != ownColor)
                {
                    moves.Add(new Move
                    {
                        From = from,
                        To = to
                    });
                }
            }
        }

        private void AddKingMoves(List<Move> moves, Square from, PieceColor ownColor)
        {
            for (int df = -1; df <= 1; df++)
            {
                for (int dr = -1; dr <= 1; dr++)
                {
                    if (df == 0 && dr == 0)
                        continue;

                    var to = new Square(from.File + df, from.Rank + dr);
                    if (!IsInside(to))
                        continue;

                    var target = GetPiece(to);
                    if (target == null || target.Color != ownColor)
                    {
                        moves.Add(new Move
                        {
                            From = from,
                            To = to
                        });
                    }
                }
            }
        }

        private void AddRayMoves(List<Move> moves, Square from, PieceColor ownColor, int df, int dr)
        {
            int file = from.File + df;
            int rank = from.Rank + dr;

            while (file >= 0 && file < 8 && rank >= 0 && rank < 8)
            {
                var to = new Square(file, rank);
                var target = GetPiece(to);

                if (target == null)
                {
                    moves.Add(new Move
                    {
                        From = from,
                        To = to
                    });
                }
                else
                {
                    if (target.Color != ownColor)
                    {
                        moves.Add(new Move
                        {
                            From = from,
                            To = to
                        });
                    }

                    break;
                }

                file += df;
                rank += dr;
            }
        }

        private static bool IsInside(Square square)
            => square.File >= 0 && square.File < 8 && square.Rank >= 0 && square.Rank < 8;

        public static BoardState CreateInitial()
        {
            var board = new BoardState();

            board.SetPiece(new Square(0, 0), new Piece(PieceType.Rook, PieceColor.White));
            board.SetPiece(new Square(1, 0), new Piece(PieceType.Knight, PieceColor.White));
            board.SetPiece(new Square(2, 0), new Piece(PieceType.Bishop, PieceColor.White));
            board.SetPiece(new Square(3, 0), new Piece(PieceType.Queen, PieceColor.White));
            board.SetPiece(new Square(4, 0), new Piece(PieceType.King, PieceColor.White));
            board.SetPiece(new Square(5, 0), new Piece(PieceType.Bishop, PieceColor.White));
            board.SetPiece(new Square(6, 0), new Piece(PieceType.Knight, PieceColor.White));
            board.SetPiece(new Square(7, 0), new Piece(PieceType.Rook, PieceColor.White));

            for (int file = 0; file < 8; file++)
                board.SetPiece(new Square(file, 1), new Piece(PieceType.Pawn, PieceColor.White));

            board.SetPiece(new Square(0, 7), new Piece(PieceType.Rook, PieceColor.Black));
            board.SetPiece(new Square(1, 7), new Piece(PieceType.Knight, PieceColor.Black));
            board.SetPiece(new Square(2, 7), new Piece(PieceType.Bishop, PieceColor.Black));
            board.SetPiece(new Square(3, 7), new Piece(PieceType.Queen, PieceColor.Black));
            board.SetPiece(new Square(4, 7), new Piece(PieceType.King, PieceColor.Black));
            board.SetPiece(new Square(5, 7), new Piece(PieceType.Bishop, PieceColor.Black));
            board.SetPiece(new Square(6, 7), new Piece(PieceType.Knight, PieceColor.Black));
            board.SetPiece(new Square(7, 7), new Piece(PieceType.Rook, PieceColor.Black));

            for (int file = 0; file < 8; file++)
                board.SetPiece(new Square(file, 6), new Piece(PieceType.Pawn, PieceColor.Black));

            return board;
        }
    }
}