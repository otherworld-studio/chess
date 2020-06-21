using System.Collections.Generic;
using UnityEngine;

using Square = Board.Square;

public abstract class Piece : MonoBehaviour
{
    // DO NOT make this readonly, we want it serialized
    public Color color;

    public abstract Type type { get; }

    public Color opponent { get { return Opponent(color); } }

    // True iff this piece can make this move on this BOARD. Assume that FROM != TO and that it is this color's turn. Doesn't care if the king is in check or is put into a checked square as a result of this move.
    public abstract bool IsLegalMove(Square from, Square to, Board board);
    // True iff a king located at TARGET would be in check due to this piece at its CURRENT position, on the current BOARD. Assume TARGET != CURRENT.
    public abstract bool IsCheckedSquare(Square target, Square current, Board board);

    public abstract IEnumerable<Square> LegalMoves(Square from, Board board);

    // Assumes the move FROM -> TO is legal, and that promotion != Type.King
    public virtual void PreMove(Square from, Square to, Board board, Type promotion = Type.Pawn, bool modifyGameObjects = true) { return; }

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