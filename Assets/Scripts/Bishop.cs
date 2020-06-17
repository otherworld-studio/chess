using System;

public class Bishop : Piece
{
    public override Type type { get { return Type.Bishop; } }

    public override bool IsLegalMove(Square from, Square to, Board board)
    {
        Piece p = board.Get(to);
        return (p == null || p.color == opponent) && IsChecking(from, to, board);
    }

    public override bool IsChecking(Square from, Square to, Board board)
    {
        return Math.Abs(to.file - from.file) == Math.Abs(to.rank - from.rank) && board.IsUnblockedPath(from, to);
    }
}
