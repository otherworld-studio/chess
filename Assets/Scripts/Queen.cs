
public class Queen : Piece
{
    public override Type type { get { return Type.Queen; } }

    public override bool IsLegalMove(Square from, Square to, Board board)
    {
        Piece p = board.Get(to);
        return (p == null || p.color == opponent) && IsChecking(from, to, board);
    }

    public override bool IsChecking(Square from, Square to, Board board)
    {
        return board.IsUnblockedPath(from, to);
    }
}
