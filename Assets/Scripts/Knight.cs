using System;
using System.Collections.Generic;

public class Knight : Piece
{
    public override Type type { get { return Type.Knight; } }

    public override bool IsLegalMove(Square from, Square to, Board board)
    {
        Piece p = board.Get(to);
        return (p == null || p.color == opponent) && IsCheckedSquare(to, from, board);
    }

    public override bool IsCheckedSquare(Square target, Square current, Board board)
    {
        int x = Math.Abs(target.file - current.file);
        int y = Math.Abs(target.rank - current.rank);

        if (x == 1)
        {
            return y == 2;
        }
        else if (x == 2)
        {
            return y == 1;
        }

        return false;
    }

    public override IEnumerable<Square> LegalMoves(Square from, Board board)
    {
        int[,] squares = { { from.file + 2, from.rank + 1 }, { from.file + 1, from.rank + 2 },
                           { from.file - 1, from.rank + 2 }, { from.file - 2, from.rank + 1 },
                           { from.file - 2, from.rank - 1 }, { from.file - 1, from.rank - 2 },
                           { from.file + 1, from.rank - 2 }, { from.file + 2, from.rank - 1 } };

        for (int i = 0; i < 8; ++i)
        {
            int x = squares[i, 0], y = squares[i, 1];
            if (!Square.Exists(x, y)) continue;

            Square to = Square.At(x, y);
            Piece p = board.Get(to);
            if (p == null || p.color == opponent) yield return to;
        }
    }
}
