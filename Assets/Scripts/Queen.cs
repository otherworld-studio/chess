
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
}
