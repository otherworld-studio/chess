using System.Collections.Generic;

public class Queen : Piece
{
    public override Type type { get { return Type.Queen; } }

    public override bool IsLegalMove(Square from, Square to, Board board)
    {
        Piece p = board.Get(to);
        return (p == null || p.color == opponent) && IsCheckedSquare(to, from, board);
    }

    public override bool IsCheckedSquare(Square target, Square current, Board board)
    {
        return board.IsUnblockedPath(current, target);
    }

    public override IEnumerable<Square> LegalMoves(Square from, Board board)
    {
        for (int dir = 0; dir < 8; ++dir)
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
