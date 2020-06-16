using System;

public class Bishop : Piece
{
    public override Type type { get { return Type.Bishop; } }

    public override bool IsLegalMove(Square from, Square to, Board board)
    {
        if (Math.Abs(to.file - from.file) != Math.Abs(to.rank - from.rank)) return false;

        Piece p = board.Get(to);
        return (p == null || p.color == opponent) && board.IsUnblockedPath(from, to);
    }
}
