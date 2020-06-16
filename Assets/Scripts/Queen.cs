
public class Queen : Piece
{
    public override Type type { get { return Type.Queen; } }

    public override bool IsLegalMove(Square from, Square to, Board board)
    {
        Piece p = board.Get(to);
        return ((p == null || p.color == opponent) && board.IsUnblockedPath(from, to, true));
    }
}
