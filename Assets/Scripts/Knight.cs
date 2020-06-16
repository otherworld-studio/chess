using System;

public class Knight : Piece
{
    public override Type type { get { return Type.Knight; } }

    public override bool IsLegalMove(Square from, Square to, Board board)
    {
        Piece p = board.Get(to);
        if (p != null && p.color == color) return false;

        int x = Math.Abs(to.file - from.file);
        int y = Math.Abs(to.rank - from.rank);

        if (x == 1)
        {
            return y == 2;
        }
        else if (x == 2)
        {
            return y == 1;
        }

        return false;
    }
}
