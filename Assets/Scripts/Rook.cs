using System.Collections.Generic;

public class Rook : Piece
{
    public override Type type { get { return Type.Rook; } }

    public override bool IsLegalMove(Square from, Square to, Board board)
    {
        Piece p = board.Get(to);
        return (p == null || p.color == opponent) && IsCheckedSquare(to, from, board);
    }

    public override bool IsCheckedSquare(Square target, Square current, Board board)
    {
        return (current.file == target.file || current.rank == target.rank) && board.IsUnblockedPath(current, target);
    }

    public override IEnumerable<Square> LegalMoves(Square from, Board board)
    {
        for (int dir = 0; dir < 8; dir += 2)
        {
            foreach (Square to in from.StraightLine(dir))
            {
                Piece p = board.Get(to);
                if (p != null)
                {
                    if (p.color == opponent) yield return to;
                    break;
                }

                yield return to;
            }
        }
    }

    public override void PreMove(Square from, Square to, Board board, Type promotion = Type.Pawn, bool modifyGameObjects = true)
    {
        board.hasMoved.Add(this);
    }
}
