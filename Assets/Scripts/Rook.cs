using System;

public class Rook : Piece
{
    public override Type type { get { return Type.Rook; } }

    [NonSerialized]
    public bool hasMoved = false;

    public override bool IsLegalMove(Square from, Square to, Board board)
    {
        Piece p = board.Get(to);
        return (p == null || p.color == opponent) && IsChecking(from, to, board);
    }

    public override bool IsChecking(Square from, Square to, Board board)
    {
        return (from.file == to.file || from.rank == to.rank) && board.IsUnblockedPath(from, to);
    }

    public override void PreMove(Square from, Square to, Board board)
    {
        hasMoved = true;
    }
}
