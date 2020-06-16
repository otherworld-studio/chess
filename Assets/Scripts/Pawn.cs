using System;

public class Pawn : Piece
{
    public override Type type { get { return Type.Pawn; } }

    public override bool IsLegalMove(Square from, Square to, Board board)
    {
        int steps = (color == Color.White) ? to.rank - from.rank : from.rank - to.rank;
        if (from.file == to.file)
        {
            return steps == 1 || steps == 2 && from.rank == ((color == Color.White) ? 1 : 6) && BOTH_SPACES_EMPTY;//TODO
        }
        if (steps == 1 && Math.Abs(to.file - from.file) == 1)
        {
            Piece p = board.Get(to);
            if (p != null) return p.color == opponent;

            // En passant
            p = board.Get(Square.At(to.file, from.rank));
            return p != null && p.color == opponent && p == board.justDoubleStepped;
        }

        return false;
    }

    public override void PreMove(Square from, Square to, Board board)
    {
        if (Math.Abs(to.rank - from.rank) == 2)
        {
            board.justDoubleStepped = this;
        }
        else
        {
            if (from.file != to.file && board.Get(to) == null)
            {
                board.Take(Square.At(to.file, from.rank)); // En passant
            }
            else if (color == Color.White)
            {
                if (to.rank == 7)
                {
                    board.Promote(from);
                }
            }
            else
            {
                if (to.rank == 0)
                {
                    board.Promote(from);
                }
            }
        }
    }
}
