using System;

public class Rook : Piece
{
    public override Type type { get { return Type.Rook; } }

    [NonSerialized]
    public bool hasMoved = false;

    public override bool IsLegalMove(Square from, Square to, Board board)
    {
        Piece p = board.Get(to);
        return (p == null || p.color == opponent) && IsCheckedSquare(to, from, board);
    }

    public override bool IsCheckedSquare(Square target, Square current, Board board)
    {
        return (current.file == target.file || current.rank == target.rank) && board.IsUnblockedPath(current, target);
    }

    public override void PreMove(Square from, Square to, Board board)
    {
        hasMoved = true;
    }
}
