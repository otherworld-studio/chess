using System;

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
}
