using System;
using UnityEngine;

using Piece = Board.PieceTag;

// Attached to each piece prefab as a component. This is how each model's location is tracked on the Board.
public class GamePiece : MonoBehaviour
{
    [NonSerialized]
    public Piece piece;
}