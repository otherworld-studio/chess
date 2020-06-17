using System;

public class King : Piece
{
    public override Type type { get { return Type.King; } }

    [NonSerialized]
    public bool hasMoved = false;

    // Assume to is unthreatened - that is checked in board.IsLegalMove
    public override bool IsLegalMove(Square from, Square to, Board board)
    {
        int x = to.file - from.file, y = to.rank - from.rank;
        if (Math.Abs(x) > 1) // Castling
        {
            if (y != 0 || hasMoved) return false;

            Square rookSquare = (x == 2) ? Square.At(7, from.rank) : (x == -2) ? Square.At(0, from.rank) : null;
            if (rookSquare == null) return false; // No valid rook present

            Piece r = board.Get(rookSquare);
            if (r.type != Type.Rook || ((Rook)r).hasMoved || !board.IsUnblockedPath(from, rookSquare) || board.IsChecked(from, opponent)) return false;

            Square newRookSquare = (x == 2) ? Square.At(5, from.rank) : Square.At(3, from.rank);
            return !board.IsChecked(newRookSquare, opponent);
        }

        if (Math.Abs(y) > 1) return false;

        Piece p = board.Get(to);
        return p == null || p.color == opponent;
    }

    public override bool IsChecking(Square from, Square to, Board board)
    {
        return Math.Abs(to.file - from.file) <= 1 && Math.Abs(to.rank - from.rank) <= 1;
    }

    public override void PreMove(Square from, Square to, Board board)
    {
        hasMoved = true;

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
