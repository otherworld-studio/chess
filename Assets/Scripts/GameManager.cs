﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using BoardStatus = Board.BoardStatus;
using Square = Board.Square;
using PieceType = Board.PieceType;
using PieceColor = Board.PieceColor;
using Piece = Board.PieceTag;

using Move = Board.Move;

// TODO: UI menus

public class GameManager : MonoBehaviour
{
    [SerializeField]
    private List<GameObject> piecePrefabs;

    [SerializeField]
    private GameObject promoteMenu, gameOverMenu;
    [SerializeField]
    private Text gameOverText, winnerText;

    public const float tileSize = 1.5f;
    public static readonly Vector3 tileRight = Vector3.right * tileSize, tileUp = Vector3.up * tileSize, tileForward = Vector3.forward * tileSize;
    public static readonly Vector3 boardCenter = 4 * (tileRight + tileForward);

    private static Board board;
    private static GamePiece[] gamePieces;

    private static Square selectedSquare;

    private static GameManager singletonInstance = null;

    void Awake()
    {
        Debug.Assert(singletonInstance == null);
        singletonInstance = this;

        board = new Board();
        gamePieces = new GamePiece[64];
    }

    void Start()
    {
        Tests();

        UpdateGameObjects();
    }

    void Update()
    {
        if (board.status != BoardStatus.Playing) return;

        Square mouseSquare = GetMouseSquare();
        Draw(mouseSquare); // DEBUG

        if (Input.GetMouseButtonDown(0) && mouseSquare != null)
        {
            Piece p = board.GetPiece(mouseSquare);
            if (p != null && p.color == board.turn)
            {
                selectedSquare = mouseSquare;
            }
            else if (selectedSquare != null)
            {
                if (board.MakeMove(selectedSquare, mouseSquare)) {
                    UpdateGameObjects();
                    switch(board.status)
                    {
                        case BoardStatus.Promote:
                            promoteMenu.SetActive(true);
                            break;
                        case BoardStatus.Checkmate:
                            gameOverText.text = "Checkmate!";
                            winnerText.text = ((board.turn == PieceColor.White) ? "White" : "Black") + " wins!";
                            gameOverMenu.SetActive(true);
                            break;
                        case BoardStatus.Stalemate:
                            gameOverText.text = "Stalemate!";
                            winnerText.text = "Draw";
                            gameOverMenu.SetActive(true);
                            break;
                    }
                }

                selectedSquare = null;
            }
        }
    }

    public void Reset()
    {
        gameOverMenu.SetActive(false);
        board.Reset();
        UpdateGameObjects();
    }

    public void Promote(int type)
    {
        Debug.Assert(board.Promote((PieceType)type));
        promoteMenu.SetActive(false);
        UpdateGameObjects();
    }

    private void UpdateGameObjects()
    {
        foreach (Square s in Square.squares)
        {
            Piece p = board.GetPiece(s);
            GamePiece g = gamePieces[8 * s.rank + s.file];
            if (p == null)
            {
                if (g != null)
                {
                    // TODO: add new location field to PieceTag? Then we could just update the position
                    Destroy(g.gameObject);
                    gamePieces[8 * s.rank + s.file] = null;
                }
            } else
            {
                if (g == null)
                {
                    Spawn(p, s);
                }
                else if (g.piece != p)
                {
                    Destroy(g.gameObject);
                    Spawn(p, s);
                }
            }
        }
    }

    private void Spawn(Piece piece, Square square)
    {
        Quaternion rotation = (piece.color == PieceColor.White) ? Quaternion.identity : Quaternion.Euler(Vector3.up * 180f);
        GamePiece p = Instantiate(piecePrefabs[(int)piece.type + 6 * (int)piece.color], GetSquareCenter(square), rotation).GetComponent<GamePiece>();
        Debug.Assert(p != null, piece.type);
        p.piece = piece;
        gamePieces[8 * square.rank + square.file] = p;
    }

    private Square GetMouseSquare()
    {
        if (!Camera.main) return null;

        RaycastHit hit;
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 25.0f, LayerMask.GetMask("Board")))
        {
            int x = (int)(hit.point.x / tileSize), y = (int)(hit.point.z / tileSize);
            return (Square.Exists(x, y)) ? Square.At(x, y) : null;
        }

        return null;
    }

    private Vector3 GetSquareCenter(Square square)
    {
        return tileRight * (square.file + 0.5f) + tileForward * (square.rank + 0.5f);
    }

    private void Draw(Square mouseSquare)
    {
        Vector3 widthLine = tileRight * 8;
        Vector3 depthLine = tileForward * 8;
        for (int i = 0; i <= 8; ++i)
        {
            Vector3 start = tileForward * i;
            Debug.DrawLine(start, start + widthLine);

        }
        for (int j = 0; j <= 8; ++j)
        {
            Vector3 start = tileRight * j;
            Debug.DrawLine(start, start + depthLine);
        }

        if (mouseSquare != null) {
            Debug.DrawLine(tileRight * mouseSquare.file + tileForward * mouseSquare.rank,
                           tileRight * (mouseSquare.file + 1) + tileForward * (mouseSquare.rank + 1));

            Debug.DrawLine(tileRight * mouseSquare.file + tileForward * (mouseSquare.rank + 1),
                           tileRight * (mouseSquare.file + 1) + tileForward * mouseSquare.rank);
        }
    }

    private void Tests()
    {
        Move[] kasparov_vs_topalov = { new Move(Square.At(4, 1), Square.At(4, 3)), new Move(Square.At(3, 6), Square.At(3, 5)),
                                       new Move(Square.At(3, 1), Square.At(3, 3)), new Move(Square.At(6, 7), Square.At(5, 5)),
                                       new Move(Square.At(1, 0), Square.At(2, 2)), new Move(Square.At(6, 6), Square.At(6, 5)),
                                       new Move(Square.At(2, 0), Square.At(4, 2)), new Move(Square.At(5, 7), Square.At(6, 6)),
                                       new Move(Square.At(3, 0), Square.At(3, 1)), new Move(Square.At(2, 6), Square.At(2, 5)),
                                       new Move(Square.At(5, 1), Square.At(5, 2)), new Move(Square.At(1, 6), Square.At(1, 4)),
                                       new Move(Square.At(6, 0), Square.At(4, 1)), new Move(Square.At(1, 7), Square.At(3, 6)),
                                       new Move(Square.At(4, 2), Square.At(7, 5)), new Move(Square.At(6, 6), Square.At(7, 5)), // First blood
                                       new Move(Square.At(3, 1), Square.At(7, 5)), new Move(Square.At(2, 7), Square.At(1, 6)),
                                       new Move(Square.At(0, 1), Square.At(0, 2)), new Move(Square.At(4, 6), Square.At(4, 4)),
                                       new Move(Square.At(4, 0), Square.At(2, 0)), new Move(Square.At(3, 7), Square.At(4, 6)), // White castle
                                       new Move(Square.At(2, 0), Square.At(1, 0)), new Move(Square.At(0, 6), Square.At(0, 5)),
                                       new Move(Square.At(4, 1), Square.At(2, 0)), new Move(Square.At(4, 7), Square.At(2, 7)), // Black castle
                                       new Move(Square.At(2, 0), Square.At(1, 2)), new Move(Square.At(4, 4), Square.At(3, 3)),
                                       new Move(Square.At(3, 0), Square.At(3, 3)), new Move(Square.At(2, 5), Square.At(2, 4)),
                                       new Move(Square.At(3, 3), Square.At(3, 0)), new Move(Square.At(3, 6), Square.At(1, 5)),
                                       new Move(Square.At(6, 1), Square.At(6, 2)), new Move(Square.At(2, 7), Square.At(1, 7)),
                                       new Move(Square.At(1, 2), Square.At(0, 4)), new Move(Square.At(1, 6), Square.At(0, 7)),
                                       new Move(Square.At(5, 0), Square.At(7, 2)), new Move(Square.At(3, 5), Square.At(3, 4)),
                                       new Move(Square.At(7, 5), Square.At(5, 3)), new Move(Square.At(1, 7), Square.At(0, 6)),
                                       new Move(Square.At(7, 0), Square.At(4, 0)), new Move(Square.At(3, 4), Square.At(3, 3)),
                                       new Move(Square.At(2, 2), Square.At(3, 4)), new Move(Square.At(1, 5), Square.At(3, 4)),
                                       new Move(Square.At(4, 3), Square.At(3, 4)), new Move(Square.At(4, 6), Square.At(3, 5)),
                                       new Move(Square.At(3, 0), Square.At(3, 3)), new Move(Square.At(2, 4), Square.At(3, 3)),
                                       new Move(Square.At(4, 0), Square.At(4, 6)), new Move(Square.At(0, 6), Square.At(1, 5)),
                                       new Move(Square.At(5, 3), Square.At(3, 3)), new Move(Square.At(1, 5), Square.At(0, 4)),
                                       new Move(Square.At(1, 1), Square.At(1, 3)), new Move(Square.At(0, 4), Square.At(0, 3)),
                                       new Move(Square.At(3, 3), Square.At(2, 2)), new Move(Square.At(3, 5), Square.At(3, 4)),
                                       new Move(Square.At(4, 6), Square.At(0, 6)), new Move(Square.At(0, 7), Square.At(1, 6)),
                                       new Move(Square.At(0, 6), Square.At(1, 6)), new Move(Square.At(3, 4), Square.At(2, 3)),
                                       new Move(Square.At(2, 2), Square.At(5, 5)), new Move(Square.At(0, 3), Square.At(0, 2)),
                                       new Move(Square.At(5, 5), Square.At(0, 5)), new Move(Square.At(0, 2), Square.At(1, 3)),
                                       new Move(Square.At(2, 1), Square.At(2, 2)), new Move(Square.At(1, 3), Square.At(2, 2)),
                                       new Move(Square.At(0, 5), Square.At(0, 0)), new Move(Square.At(2, 2), Square.At(3, 1)),
                                       new Move(Square.At(0, 0), Square.At(1, 1)), new Move(Square.At(3, 1), Square.At(3, 0)),
                                       new Move(Square.At(7, 2), Square.At(5, 0)), new Move(Square.At(3, 7), Square.At(3, 1)),
                                       new Move(Square.At(1, 6), Square.At(3, 6)), new Move(Square.At(3, 1), Square.At(3, 6)),
                                       new Move(Square.At(5, 0), Square.At(2, 3)), new Move(Square.At(1, 4), Square.At(2, 3)), // Black queen
                                       new Move(Square.At(1, 1), Square.At(7, 7)), new Move(Square.At(3, 6), Square.At(3, 2)),
                                       new Move(Square.At(7, 7), Square.At(0, 7)), new Move(Square.At(2, 3), Square.At(2, 2)),
                                       new Move(Square.At(0, 7), Square.At(0, 3)), new Move(Square.At(3, 0), Square.At(4, 0)),
                                       new Move(Square.At(5, 2), Square.At(5, 3)), new Move(Square.At(5, 6), Square.At(5, 4)),
                                       new Move(Square.At(1, 0), Square.At(2, 0)), new Move(Square.At(3, 2), Square.At(3, 1)),
                                       new Move(Square.At(0, 3), Square.At(0, 6)) };

        for (int i = 0; i < 87; ++i)
        {
            Move m = kasparov_vs_topalov[i];
            Debug.Assert(board.GetPiece(m.from) != null, i);
            Debug.Assert(board.MakeMove(m.from, m.to), i);
        }

        board.Reset();

        Move[] morphy_vs_allies = { new Move(Square.At(4, 1), Square.At(4, 3)), new Move(Square.At(4, 6), Square.At(4, 5)),
                                    new Move(Square.At(3, 1), Square.At(3, 3)), new Move(Square.At(3, 6), Square.At(3, 4)),
                                    new Move(Square.At(4, 3), Square.At(3, 4)), new Move(Square.At(4, 5), Square.At(3, 4)),
                                    new Move(Square.At(6, 0), Square.At(5, 2)), new Move(Square.At(6, 7), Square.At(5, 5)),
                                    new Move(Square.At(5, 0), Square.At(3, 2)), new Move(Square.At(5, 7), Square.At(3, 5)),
                                    new Move(Square.At(4, 0), Square.At(6, 0)), new Move(Square.At(4, 7), Square.At(6, 7)), // Castles
                                    new Move(Square.At(1, 0), Square.At(2, 2)), new Move(Square.At(2, 6), Square.At(2, 4)),
                                    new Move(Square.At(3, 3), Square.At(2, 4)), new Move(Square.At(3, 5), Square.At(2, 4)),
                                    new Move(Square.At(2, 0), Square.At(6, 4)), new Move(Square.At(2, 7), Square.At(4, 5)),
                                    new Move(Square.At(3, 0), Square.At(3, 1)), new Move(Square.At(1, 7), Square.At(2, 5)),
                                    new Move(Square.At(0, 0), Square.At(3, 0)), new Move(Square.At(2, 4), Square.At(4, 6)),
                                    new Move(Square.At(5, 0), Square.At(4, 0)), new Move(Square.At(0, 6), Square.At(0, 5)),
                                    new Move(Square.At(3, 1), Square.At(5, 3)), new Move(Square.At(5, 5), Square.At(7, 4)),
                                    new Move(Square.At(5, 3), Square.At(7, 3)), new Move(Square.At(6, 6), Square.At(6, 5)),
                                    new Move(Square.At(6, 1), Square.At(6, 3)), new Move(Square.At(7, 4), Square.At(5, 5)),
                                    new Move(Square.At(7, 1), Square.At(7, 2)), new Move(Square.At(0, 7), Square.At(2, 7)),
                                    new Move(Square.At(0, 1), Square.At(0, 2)), new Move(Square.At(5, 7), Square.At(4, 7)),
                                    new Move(Square.At(2, 2), Square.At(4, 1)), new Move(Square.At(7, 6), Square.At(7, 4)),
                                    new Move(Square.At(4, 1), Square.At(5, 3)), new Move(Square.At(5, 5), Square.At(7, 6)),
                                    new Move(Square.At(5, 3), Square.At(4, 5)), new Move(Square.At(5, 6), Square.At(4, 5)),
                                    new Move(Square.At(4, 0), Square.At(4, 5)), new Move(Square.At(4, 6), Square.At(6, 4)),
                                    new Move(Square.At(4, 5), Square.At(6, 5)), new Move(Square.At(6, 7), Square.At(5, 7)),
                                    new Move(Square.At(7, 3), Square.At(7, 4)), new Move(Square.At(2, 7), Square.At(2, 6)),
                                    new Move(Square.At(5, 2), Square.At(6, 4)), new Move(Square.At(4, 7), Square.At(4, 6)),
                                    new Move(Square.At(7, 4), Square.At(7, 5)), new Move(Square.At(5, 7), Square.At(4, 7)),
                                    new Move(Square.At(6, 5), Square.At(6, 7)) };

        for (int i = 0; i < 51; ++i)
        {
            Move m = morphy_vs_allies[i];
            Debug.Assert(board.GetPiece(m.from) != null, i);
            Debug.Assert(board.MakeMove(m.from, m.to), i);
        }

        board.Reset();
    }
}