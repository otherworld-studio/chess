using UnityEngine;

public abstract class Piece : MonoBehaviour
{
    // DO NOT make this readonly because we want it serialized
    public Color color;

    public abstract Type type { get; }

    public Color opponent { get { return Opponent(color); } }

    // True iff this piece can make this move on this BOARD. Assume that FROM != TO and that it is this color's turn.
    public abstract bool IsLegalMove(Square from, Square to, Board board);
    // True iff a king at square TO would be checked by this piece at square FROM. Assume FROM != TO.
    public abstract bool IsChecking(Square from, Square to, Board board);

    public virtual void PreMove(Square from, Square to, Board board) { return; }

    public enum Type
    {
        Pawn = 0,
        Knight = 1,
        Bishop = 2,
        Rook = 3,
        Queen = 4,
        King = 5
    }

    public enum Color
    {
        White = 0,
        Black = 1
    }

    public static Color Opponent(Color color)
    {
        return (color == Color.White) ? Color.Black : Color.White;
    }
}