using System;
using System.Collections.Generic;

public class King : Piece
{
    public override Type type { get { return Type.King; } }

    // Assume that TO is a safe square - that is verified in board.IsLegalMove. Do not assume that it is empty.
    public override bool IsLegalMove(Square from, Square to, Board board)
    {
        int x = to.file - from.file, y = to.rank - from.rank;
        if (Math.Abs(x) > 1) // Castling
        {
            if (y != 0 || board.hasMoved.Contains(this)) return false;

            Square rookSquare = (x == 2) ? Square.At(7, from.rank) : (x == -2) ? Square.At(0, from.rank) : null;
            if (rookSquare == null) return false; // This is not the correct distance for a castle

            Piece r = board.Get(rookSquare);
            if (r.type != Type.Rook || board.hasMoved.Contains(r) || !board.IsUnblockedPath(from, rookSquare) || board.IsCheckedSquare(from, opponent)) return false;

            Square newRookSquare = (x == 2) ? Square.At(5, from.rank) : Square.At(3, from.rank);
            return !board.IsCheckedSquare(newRookSquare, opponent);
        }

        if (Math.Abs(y) > 1) return false;

        Piece p = board.Get(to);
        return p == null || p.color == opponent;
    }

    public override bool IsCheckedSquare(Square target, Square current, Board board)
    {
        return Math.Abs(target.file - current.file) <= 1 && Math.Abs(target.rank - current.rank) <= 1;
    }

    public override IEnumerable<Square> LegalMoves(Square from, Board board)
    {
        int[,] squares = { { from.file + 1, from.rank }, { from.file + 1, from.rank + 1 },
                           { from.file, from.rank + 1 }, { from.file - 1, from.rank + 1 },
                           { from.file - 1, from.rank }, { from.file - 1, from.rank - 1 },
                           { from.file, from.rank - 1 }, { from.file + 1, from.rank - 1 } };

        for (int i = 0; i < 8; ++i)
        {
            int x = squares[i, 0], y = squares[i, 1];
            if (!Square.Exists(x, y)) continue;

            Square to = Square.At(x, y);
            Piece p = board.Get(to);
            if (p == null || p.color == opponent) yield return to;
        }

        // Castling
        if (board.hasMoved.Contains(this)) yield break;

        Square[] rookSquares = { Square.At(0, from.rank), Square.At(7, from.rank) };
        Square[] newRookSquares = { Square.At(3, from.rank), Square.At(5, from.rank) };
        Square[] newKingSquares = { Square.At(2, from.rank), Square.At(6, from.rank) };
        for (int i = 0; i < 2; ++i)
        {
            Piece p = board.Get(rookSquares[i]);
            if (p.type == Type.Rook && !board.hasMoved.Contains(p) && board.IsUnblockedPath(from, rookSquares[i]) && !board.IsCheckedSquare(from, opponent) && !board.IsCheckedSquare(newRookSquares[i], opponent))
            {
                yield return newKingSquares[i];
            }
        }
    }

    public override void PreMove(Square from, Square to, Board board, Type promotion = Type.Pawn, bool modifyGameObjects = true)
    {
        board.hasMoved.Add(this);

        int x = to.file - from.file;
        if (Math.Abs(x) > 1) // Castling
        {
            if (x == 2)
            {
                board.MakeMove(Square.At(7, from.rank), Square.At(5, from.rank), modifyGameObjects: modifyGameObjects);
            }
            else
            {
                board.MakeMove(Square.At(0, from.rank), Square.At(3, from.rank), modifyGameObjects: modifyGameObjects);
            }
        }
    }
}
