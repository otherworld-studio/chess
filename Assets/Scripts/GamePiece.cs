using System;
using UnityEngine;

using Piece = Board.PieceTag;

public class GamePiece : MonoBehaviour
{
    [NonSerialized]
    public Piece piece;
}