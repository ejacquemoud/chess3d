using System.Text;
using Chess3D.Core.Enums;

namespace Chess3D.Core.Models;

public enum GameEndState
{
    None = 0,
    Check,
    Checkmate,
    Stalemate
}

public sealed class BoardState
{
    private readonly Piece?[,] _board = new Piece?[8, 8];

    public PieceColor SideToMove { get; private set; } = PieceColor.White;

    public bool WhiteCanCastleKingSide { get; private set; } = true;
    public bool WhiteCanCastleQueenSide { get; private set; } = true;
    public bool BlackCanCastleKingSide { get; private set; } = true;
    public bool BlackCanCastleQueenSide { get; private set; } = true;

    public Square? EnPassantTargetSquare { get; private set; }
    public int HalfmoveClock { get; private set; }
    public int FullmoveNumber { get; private set; } = 1;

    public Piece? GetPiece(Square square) => _board[square.File, square.Rank];

    public void SetPiece(Square square, Piece? piece) => _board[square.File, square.Rank] = piece;

    public Piece?[,] Snapshot()
    {
        var copy = new Piece?[8, 8];
        Array.Copy(_board, copy, _board.Length);
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
        var piece = GetPiece(from);
        if (piece == null || piece.Color != SideToMove)
            return Array.Empty<Move>();

        return GeneratePseudoLegalMovesForIgnoringTurn(from, piece, includeCastling: true);
    }

    public IReadOnlyList<Move> GenerateLegalMovesFor(Square from)
    {
        var piece = GetPiece(from);
        if (piece == null || piece.Color != SideToMove)
            return Array.Empty<Move>();

        var pseudoMoves = GeneratePseudoLegalMovesForIgnoringTurn(from, piece, includeCastling: true);
        var legalMoves = new List<Move>();

        foreach (var move in pseudoMoves)
        {
            var clone = Clone();
            clone.MakeMove(move);

            if (!clone.IsKingInCheck(piece.Color))
                legalMoves.Add(move);
        }

        return legalMoves;
    }

    public IReadOnlyList<Move> GenerateLegalMovesForSide(PieceColor color)
    {
        var moves = new List<Move>();

        foreach (var (square, piece) in GetOccupiedSquares())
        {
            if (piece.Color != color)
                continue;

            var pseudoMoves = GeneratePseudoLegalMovesForIgnoringTurn(square, piece, includeCastling: true);
            foreach (var move in pseudoMoves)
            {
                var clone = Clone();
                clone.MakeMove(move);

                if (!clone.IsKingInCheck(color))
                    moves.Add(move);
            }
        }

        return moves;
    }

    public bool IsKingInCheck(PieceColor color)
    {
        Square? kingSquare = null;

        foreach (var (square, piece) in GetOccupiedSquares())
        {
            if (piece.Color == color && piece.Type == PieceType.King)
            {
                kingSquare = square;
                break;
            }
        }

        if (kingSquare is null)
            return false;

        var enemyColor = color == PieceColor.White ? PieceColor.Black : PieceColor.White;
        return IsSquareAttacked(kingSquare.Value, enemyColor);
    }

    public GameEndState GetGameEndState()
    {
        bool inCheck = IsKingInCheck(SideToMove);
        bool hasAnyLegalMove = GenerateLegalMovesForSide(SideToMove).Count > 0;

        if (inCheck && !hasAnyLegalMove)
            return GameEndState.Checkmate;

        if (!inCheck && !hasAnyLegalMove)
            return GameEndState.Stalemate;

        if (inCheck)
            return GameEndState.Check;

        return GameEndState.None;
    }

    public void MakeMove(Move move)
    {
        var piece = GetPiece(move.From);
        if (piece == null)
            return;

        var targetPiece = GetPiece(move.To);
        bool isPawnMove = piece.Type == PieceType.Pawn;
        bool isCapture = targetPiece != null;
        bool isEnPassant = false;
        bool isCastling = false;

        if (isPawnMove && EnPassantTargetSquare.HasValue && move.To == EnPassantTargetSquare.Value && move.From.File != move.To.File && targetPiece == null)
        {
            isEnPassant = true;
            var capturedPawnSquare = new Square(move.To.File, move.From.Rank);
            targetPiece = GetPiece(capturedPawnSquare);
            SetPiece(capturedPawnSquare, null);
            isCapture = targetPiece != null;
        }

        UpdateCastlingRightsBeforeMove(piece, move, targetPiece);

        if (piece.Type == PieceType.King && Math.Abs(move.To.File - move.From.File) == 2)
        {
            isCastling = true;
            ApplyCastlingRookMove(piece.Color, move);
        }

        SetPiece(move.From, null);

        if (piece.Type == PieceType.Pawn && ReachesLastRank(piece.Color, move.To.Rank))
        {
            var promotedType = move.Promotion == PieceType.None ? PieceType.Queen : move.Promotion;
            SetPiece(move.To, new Piece(promotedType, piece.Color));
        }
        else
        {
            SetPiece(move.To, piece);
        }

        EnPassantTargetSquare = null;

        if (piece.Type == PieceType.Pawn && Math.Abs(move.To.Rank - move.From.Rank) == 2)
        {
            EnPassantTargetSquare = new Square(move.From.File, (move.From.Rank + move.To.Rank) / 2);
        }

        HalfmoveClock = (isPawnMove || isCapture || isEnPassant) ? 0 : HalfmoveClock + 1;

        if (SideToMove == PieceColor.Black)
            FullmoveNumber++;

        ToggleSideToMove();
    }

    public string ToFen()
    {
        var sb = new StringBuilder();

        for (int rank = 7; rank >= 0; rank--)
        {
            int emptyCount = 0;

            for (int file = 0; file < 8; file++)
            {
                var piece = _board[file, rank];
                if (piece == null)
                {
                    emptyCount++;
                    continue;
                }

                if (emptyCount > 0)
                {
                    sb.Append(emptyCount);
                    emptyCount = 0;
                }

                sb.Append(GetFenPieceChar(piece));
            }

            if (emptyCount > 0)
                sb.Append(emptyCount);

            if (rank > 0)
                sb.Append('/');
        }

        sb.Append(' ');
        sb.Append(SideToMove == PieceColor.White ? 'w' : 'b');
        sb.Append(' ');
        sb.Append(GetCastlingRightsFen());
        sb.Append(' ');
        sb.Append(EnPassantTargetSquare?.ToString() ?? "-");
        sb.Append(' ');
        sb.Append(HalfmoveClock);
        sb.Append(' ');
        sb.Append(FullmoveNumber);

        return sb.ToString();
    }

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

    private IReadOnlyList<Move> GeneratePseudoLegalMovesForIgnoringTurn(Square from, Piece piece, bool includeCastling)
    {
        var moves = new List<Move>();

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
                AddKingMoves(moves, from, piece.Color, includeCastling);
                break;
        }

        return moves;
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
                moves.Add(new Move { From = from, To = twoForward });
        }

        AddPawnCaptureOrEnPassant(moves, from, new Square(from.File - 1, from.Rank + direction), piece.Color);
        AddPawnCaptureOrEnPassant(moves, from, new Square(from.File + 1, from.Rank + direction), piece.Color);
    }

    private void AddPawnAdvanceMove(List<Move> moves, Square from, Square to, PieceColor color)
    {
        if (ReachesLastRank(color, to.Rank))
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

    private void AddPawnCaptureOrEnPassant(List<Move> moves, Square from, Square to, PieceColor ownColor)
    {
        if (!IsInside(to))
            return;

        var target = GetPiece(to);
        bool isEnPassant = EnPassantTargetSquare.HasValue && EnPassantTargetSquare.Value == to;

        if ((target == null && !isEnPassant) || (target != null && target.Color == ownColor))
            return;

        if (ReachesLastRank(ownColor, to.Rank))
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
            ( 1,  2), ( 2,  1), ( 2, -1), ( 1, -2),
            (-1, -2), (-2, -1), (-2,  1), (-1,  2)
        };

        foreach (var (df, dr) in offsets)
        {
            var to = new Square(from.File + df, from.Rank + dr);
            if (!IsInside(to))
                continue;

            var target = GetPiece(to);
            if (target == null || target.Color != ownColor)
                moves.Add(new Move { From = from, To = to });
        }
    }

    private void AddKingMoves(List<Move> moves, Square from, PieceColor ownColor, bool includeCastling)
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
                    moves.Add(new Move { From = from, To = to });
            }
        }

        if (includeCastling)
            AddCastlingMoves(moves, from, ownColor);
    }

    private void AddCastlingMoves(List<Move> moves, Square from, PieceColor color)
    {
        if (IsKingInCheck(color))
            return;

        int homeRank = color == PieceColor.White ? 0 : 7;
        if (from != new Square(4, homeRank))
            return;

        if (color == PieceColor.White)
        {
            if (WhiteCanCastleKingSide && CanCastleKingSide(color))
                moves.Add(new Move { From = from, To = new Square(6, homeRank) });

            if (WhiteCanCastleQueenSide && CanCastleQueenSide(color))
                moves.Add(new Move { From = from, To = new Square(2, homeRank) });
        }
        else
        {
            if (BlackCanCastleKingSide && CanCastleKingSide(color))
                moves.Add(new Move { From = from, To = new Square(6, homeRank) });

            if (BlackCanCastleQueenSide && CanCastleQueenSide(color))
                moves.Add(new Move { From = from, To = new Square(2, homeRank) });
        }
    }

    private bool CanCastleKingSide(PieceColor color)
    {
        int rank = color == PieceColor.White ? 0 : 7;
        var rookSquare = new Square(7, rank);
        var rook = GetPiece(rookSquare);

        if (rook == null || rook.Type != PieceType.Rook || rook.Color != color)
            return false;

        if (GetPiece(new Square(5, rank)) != null || GetPiece(new Square(6, rank)) != null)
            return false;

        var enemyColor = color == PieceColor.White ? PieceColor.Black : PieceColor.White;
        if (IsSquareAttacked(new Square(5, rank), enemyColor) || IsSquareAttacked(new Square(6, rank), enemyColor))
            return false;

        return true;
    }

    private bool CanCastleQueenSide(PieceColor color)
    {
        int rank = color == PieceColor.White ? 0 : 7;
        var rookSquare = new Square(0, rank);
        var rook = GetPiece(rookSquare);

        if (rook == null || rook.Type != PieceType.Rook || rook.Color != color)
            return false;

        if (GetPiece(new Square(1, rank)) != null || GetPiece(new Square(2, rank)) != null || GetPiece(new Square(3, rank)) != null)
            return false;

        var enemyColor = color == PieceColor.White ? PieceColor.Black : PieceColor.White;
        if (IsSquareAttacked(new Square(3, rank), enemyColor) || IsSquareAttacked(new Square(2, rank), enemyColor))
            return false;

        return true;
    }

    private bool IsSquareAttacked(Square targetSquare, PieceColor attackingColor)
    {
        foreach (var (from, piece) in GetOccupiedSquares())
        {
            if (piece.Color != attackingColor)
                continue;

            if (piece.Type == PieceType.Pawn)
            {
                int direction = attackingColor == PieceColor.White ? 1 : -1;
                if (from.Rank + direction == targetSquare.Rank && Math.Abs(from.File - targetSquare.File) == 1)
                    return true;

                continue;
            }

            if (piece.Type == PieceType.King)
            {
                if (Math.Abs(from.File - targetSquare.File) <= 1 && Math.Abs(from.Rank - targetSquare.Rank) <= 1)
                    return true;

                continue;
            }

            foreach (var move in GeneratePseudoLegalMovesForIgnoringTurn(from, piece, includeCastling: false))
            {
                if (move.To == targetSquare)
                    return true;
            }
        }

        return false;
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
                moves.Add(new Move { From = from, To = to });
            }
            else
            {
                if (target.Color != ownColor)
                    moves.Add(new Move { From = from, To = to });

                break;
            }

            file += df;
            rank += dr;
        }
    }

    private void ApplyCastlingRookMove(PieceColor color, Move kingMove)
    {
        int rank = color == PieceColor.White ? 0 : 7;

        if (kingMove.To.File == 6)
        {
            var rookFrom = new Square(7, rank);
            var rookTo = new Square(5, rank);
            var rook = GetPiece(rookFrom);
            SetPiece(rookFrom, null);
            SetPiece(rookTo, rook);
        }
        else if (kingMove.To.File == 2)
        {
            var rookFrom = new Square(0, rank);
            var rookTo = new Square(3, rank);
            var rook = GetPiece(rookFrom);
            SetPiece(rookFrom, null);
            SetPiece(rookTo, rook);
        }
    }

    private void UpdateCastlingRightsBeforeMove(Piece movingPiece, Move move, Piece? capturedPiece)
    {
        if (movingPiece.Type == PieceType.King)
        {
            if (movingPiece.Color == PieceColor.White)
            {
                WhiteCanCastleKingSide = false;
                WhiteCanCastleQueenSide = false;
            }
            else
            {
                BlackCanCastleKingSide = false;
                BlackCanCastleQueenSide = false;
            }
        }

        if (movingPiece.Type == PieceType.Rook)
        {
            if (movingPiece.Color == PieceColor.White)
            {
                if (move.From == new Square(0, 0)) WhiteCanCastleQueenSide = false;
                if (move.From == new Square(7, 0)) WhiteCanCastleKingSide = false;
            }
            else
            {
                if (move.From == new Square(0, 7)) BlackCanCastleQueenSide = false;
                if (move.From == new Square(7, 7)) BlackCanCastleKingSide = false;
            }
        }

        if (capturedPiece?.Type == PieceType.Rook)
        {
            if (capturedPiece.Color == PieceColor.White)
            {
                if (move.To == new Square(0, 0)) WhiteCanCastleQueenSide = false;
                if (move.To == new Square(7, 0)) WhiteCanCastleKingSide = false;
            }
            else
            {
                if (move.To == new Square(0, 7)) BlackCanCastleQueenSide = false;
                if (move.To == new Square(7, 7)) BlackCanCastleKingSide = false;
            }
        }
    }

    private static bool ReachesLastRank(PieceColor color, int rank)
        => (color == PieceColor.White && rank == 7) || (color == PieceColor.Black && rank == 0);

    private static bool IsInside(Square square)
        => square.File >= 0 && square.File < 8 && square.Rank >= 0 && square.Rank < 8;

    private void ToggleSideToMove()
    {
        SideToMove = SideToMove == PieceColor.White ? PieceColor.Black : PieceColor.White;
    }

    private string GetCastlingRightsFen()
    {
        var sb = new StringBuilder();

        if (WhiteCanCastleKingSide) sb.Append('K');
        if (WhiteCanCastleQueenSide) sb.Append('Q');
        if (BlackCanCastleKingSide) sb.Append('k');
        if (BlackCanCastleQueenSide) sb.Append('q');

        return sb.Length == 0 ? "-" : sb.ToString();
    }

    private static char GetFenPieceChar(Piece piece)
    {
        char c = piece.Type switch
        {
            PieceType.Pawn => 'p',
            PieceType.Knight => 'n',
            PieceType.Bishop => 'b',
            PieceType.Rook => 'r',
            PieceType.Queen => 'q',
            PieceType.King => 'k',
            _ => throw new ArgumentOutOfRangeException(nameof(piece.Type), piece.Type, null)
        };

        return piece.Color == PieceColor.White ? char.ToUpperInvariant(c) : c;
    }

    private BoardState Clone()
    {
        var clone = new BoardState
        {
            SideToMove = SideToMove,
            WhiteCanCastleKingSide = WhiteCanCastleKingSide,
            WhiteCanCastleQueenSide = WhiteCanCastleQueenSide,
            BlackCanCastleKingSide = BlackCanCastleKingSide,
            BlackCanCastleQueenSide = BlackCanCastleQueenSide,
            EnPassantTargetSquare = EnPassantTargetSquare,
            HalfmoveClock = HalfmoveClock,
            FullmoveNumber = FullmoveNumber
        };

        for (int file = 0; file < 8; file++)
        {
            for (int rank = 0; rank < 8; rank++)
            {
                var piece = _board[file, rank];
                clone._board[file, rank] = piece == null
                    ? null
                    : new Piece(piece.Type, piece.Color);
            }
        }

        return clone;
    }
}