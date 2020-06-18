using System;

public class King : Piece
{
    public override Type type { get { return Type.King; } }

    // Assume that TO is a safe square - that is verified in board.IsLegalMove. Do not assume that it is empty.
    public override bool IsLegalMove(Square from, Square to, Board board)
    {
        int x = to.file - from.file, y = to.rank - from.rank;
        if (Math.Abs(x) > 1) // Castling
        {
            if (y != 0 || board.hasMoved.Contains(this)) return false;

            Square rookSquare = (x == 2) ? Square.At(7, from.rank) : (x == -2) ? Square.At(0, from.rank) : null;
            if (rookSquare == null) return false; // No valid rook present

            Piece r = board.Get(rookSquare);
            if (r.type != Type.Rook || board.hasMoved.Contains(r) || !board.IsUnblockedPath(from, rookSquare) || board.IsCheckedSquare(from, opponent)) return false;

            Square newRookSquare = (x == 2) ? Square.At(5, from.rank) : Square.At(3, from.rank);
            return !board.IsCheckedSquare(newRookSquare, opponent);
        }

        if (Math.Abs(y) > 1) return false;

        Piece p = board.Get(to);
        return p == null || p.color == opponent;
    }

    public override bool IsCheckedSquare(Square target, Square current, Board board)
    {
        return Math.Abs(target.file - current.file) <= 1 && Math.Abs(target.rank - current.rank) <= 1;
    }

    public override IEnumerable<Square> LegalMoves(Square from, Board board)
    {
        //TODO
    }

    public override void PreMove(Square from, Square to, Board board)
    {
        board.hasMoved.Add(this);

        int x = to.file - from.file;
        if (Math.Abs(x) > 1) // Castling
        {
            if (x == 2)
            {
                board.Move(Square.At(7, from.rank), Square.At(5, from.rank));
            }
            else
            {
                board.Move(Square.At(0, from.rank), Square.At(3, from.rank));
            }
        }
    }
}
