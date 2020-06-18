using System;
using System.Collections.Generic;

public class Bishop : Piece
{
    public override Type type { get { return Type.Bishop; } }

    public override bool IsLegalMove(Square from, Square to, Board board)
    {
        Piece p = board.Get(to);
        return (p == null || p.color == opponent) && IsCheckedSquare(to, from, board);
    }

    public override bool IsCheckedSquare(Square target, Square current, Board board)
    {
        return Math.Abs(target.file - current.file) == Math.Abs(target.rank - current.rank) && board.IsUnblockedPath(current, target);
    }

    public override IEnumerable<Square> LegalMoves(Square from, Board board)
    {
        for (int dir = 1; dir < 8; dir += 2)
        {
            foreach (Square to in from.StraightLine(dir))
            {
                Piece p = board.Get(to);
                if (p != null)
                {
                    if (p.color == opponent) yield return to;
                    break;
                }

                yield return to;
            }
        }
    }
}


