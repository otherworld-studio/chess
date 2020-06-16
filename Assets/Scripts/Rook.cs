using System;

public class Rook : Piece
{
    public override Type type { get { return Type.Rook; } }

    [NonSerialized]
    public bool hasMoved = false;

    public override bool IsLegalMove(Square from, Square to, Board board)
    {
        if (from.file != to.file && from.rank != to.rank) return false;

        Piece p = board.Get(to);
        return (p == null || p.color == opponent) && board.IsUnblockedPath(from, to);
    }

    public override void PreMove(Square from, Square to, Board board)
    {
        hasMoved = true;
    }
}
