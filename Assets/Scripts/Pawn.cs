using System;

public class Pawn : Piece
{
    public override Type type { get { return Type.Pawn; } }

    //Assume from != to
    public override bool IsLegalMove(Square from, Square to, Board board)
    {
        int steps = (color == Color.White) ? to.rank - from.rank : from.rank - to.rank;
        if (from.file == to.file)
        {
            return (steps == 1 || steps == 2 && from.rank == ((color == Color.White) ? 1 : 6) && board.IsUnblockedPath(from, to, false));
        }
        if (steps == 1 && Math.Abs(to.file - from.file) == 1)
        {
            Piece p = board.Get(to);
            if (p != null) return p.color == opponent;

            //En passant
            p = board.Get(to.file, (color == Color.White) ? to.rank - 1 : to.rank + 1);
            return (p != null && p.color == opponent && p == board.justDoubleStepped);
        }

        return false;
    }

    public override void Move(Square from, Square to, Board board)
    {
        if (Math.Abs(to.rank - from.rank) == 2)
        {
            board.justDoubleStepped = this;
        }
        else
        {
            if (color == Color.White)
            {
                if (to.rank == 7) board.Promote(this);
            }
            else
            {
                if (to.rank == 0) board.Promote(this);
            }
        }
    }
}
