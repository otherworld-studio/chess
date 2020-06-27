using System;
using UnityEngine;

using Piece = Board.PieceTag;

// Attached to each piece prefab as a component
public class GamePiece : MonoBehaviour
{
    [SerializeField]
    private Renderer renderer;

    [NonSerialized]
    public Piece piece; // how we track the piece's location on the Board.

    private Color startColor;

    public void Awake()
    {
        startColor = renderer.material.color;
    }

    public void Highlight(bool value)
    {
        if (value)
        {
            renderer.material.color = Color.yellow;
        } else
        {
            renderer.material.color = startColor;
        }
    }
}