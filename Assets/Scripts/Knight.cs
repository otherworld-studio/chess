using System;

public class Knight : Piece
{
    public override Type type { get { return Type.Knight; } }

    public override bool IsLegalMove(Square from, Square to, Board board)
    {
        Piece p = board.Get(to);
        return (p == null || p.color == opponent) && IsCheckedSquare(to, from, board);
    }

    public override bool IsCheckedSquare(Square target, Square current, Board board)
    {
        int x = Math.Abs(target.file - current.file);
        int y = Math.Abs(target.rank - current.rank);

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
