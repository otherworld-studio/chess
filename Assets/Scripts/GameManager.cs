using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using BoardStatus = Board.BoardStatus;
using Square = Board.Square;
using PieceType = Board.PieceType;
using PieceColor = Board.PieceColor;
using PieceData = Board.PieceData;

using Move = Board.Move;

// TODO:
// improve piece highlighting: https://forum.unity.com/threads/solved-gameobject-picking-highlighting-and-outlining.40407/
// animate promote buttons on piece placement
// outline square on mouseover (color of outline can be dependent on piece conditions, e.g. legality)
// additional Board draw conditions (threefold repetition, impossible endgame conditions, fifty moves, etc.)
// online multiplayer
// draw by mutual agreement (GameManager)
// show pieces that have been taken on the side of the board

public class GameManager : MonoBehaviour
{
    [SerializeField]
    private List<GamePiece> piecePrefabs;

    [SerializeField]
    private Text turnText, gameOverText, winnerText, debugText;
    [SerializeField]
    private GameObject boardObject, gameOverMenu;

    private static GameManager instance = null; // singleton instance

    private Board board;
    private GamePiece[] gamePieces;
    private bool resigned;

    private Square _selectedSquare;
    private Square selectedSquare {
        get { return _selectedSquare; }
        set {
            if (value == _selectedSquare) return;

            if (_selectedSquare != null) // a piece was already selected
            {
                GamePiece selectedPiece = Get(_selectedSquare);
                selectedPiece.transform.position = GetSquareCenter(_selectedSquare); // If UpdateScene is called afterward (on a successful move) this line won't matter anyway
                selectedPiece.Select(false);
            }

            _selectedSquare = value;
            if (_selectedSquare != null)
            {
                GamePiece selectedPiece = Get(_selectedSquare);
                selectedPiece.Highlight(false);
                selectedPiece.Select(true);
            }
        }
    }

    private Square _mouseSquare;
    private Square mouseSquare
    {
        get { return _mouseSquare; }
        set
        {
            if (value == _mouseSquare) return;
            
            if (_mouseSquare != null)
            {
                GamePiece mousePiece = Get(_mouseSquare);
                if (mousePiece != null) mousePiece.Highlight(false);
            }

            _mouseSquare = value;
            if (_mouseSquare != null)
            {
                if (_selectedSquare != null) Get(_selectedSquare).transform.position = GetSquareCenter(_mouseSquare);

                if (_mouseSquare != _selectedSquare)
                {
                    GamePiece mousePiece = Get(_mouseSquare);
                    if (mousePiece != null) mousePiece.Highlight(true);
                }
            }
        }
    }

    public const float tileSize = 1.5f;
    public static readonly Vector3 tileRight = Vector3.right * tileSize, tileUp = Vector3.up * tileSize, tileForward = Vector3.forward * tileSize;

    public static Vector3 boardCenter { get { return instance.boardObject.transform.position; } }
    public static Vector3 boardCorner { get { return boardCenter - 4 * (tileRight + tileForward); } }

    void Awake()
    {
        Debug.Assert(instance == null);
        instance = this;

        gamePieces = new GamePiece[64];

        Reset();
    }

    void Update()
    {
        if (board.status != BoardStatus.Playing || resigned) return;

        mouseSquare = GetMouseSquare();
        DrawDebugLines(); // DEBUG

        if (Input.GetMouseButtonDown(0))
        {
            if (mouseSquare != null)
            {
                if (selectedSquare == null) // clicked on the board, but no previously selected square
                {
                    PieceData p = board.GetPiece(mouseSquare);
                    if (p != null && p.color == board.whoseTurn) selectedSquare = mouseSquare; // only select if it's one of our pieces
                }
                else if (mouseSquare == selectedSquare)
                {
                    selectedSquare = null;
                }
                else // the player has attempted to make a move
                {
                    Move move = new Move(selectedSquare, mouseSquare);
                    if (board.IsLegalMove(move))
                    {
                        board.MakeMove(move);
                        selectedSquare = null; // piece must be unselected before any calls to UpdateScene()
                        UpdateScene();
                        switch (board.status)
                        {
                            case BoardStatus.Promote:
                                Get(board.needsPromotion).RequestPromotion();
                                break;
                            case BoardStatus.Checkmate:
                                gameOverText.text = "Checkmate!";
                                if (board.whoseTurn == PieceColor.White)
                                {
                                    winnerText.text = "White wins!";
                                    winnerText.color = Color.white;
                                }
                                else
                                {
                                    winnerText.text = "Black wins!";
                                    winnerText.color = Color.black;
                                }
                                gameOverMenu.SetActive(true);
                                break;
                            case BoardStatus.Stalemate:
                                gameOverText.text = "Stalemate!";
                                winnerText.text = "Draw";
                                winnerText.color = (board.whoseTurn == PieceColor.White) ? Color.white : Color.black;
                                // TODO: disable HUDs
                                gameOverMenu.SetActive(true);
                                break;
                        }
                    }
                }
            }
        }
    }

    private GamePiece Get(Square square)
    {
        return gamePieces[8 * square.rank + square.file];
    }

    private void Set(Square square, GamePiece piece)
    {
        gamePieces[8 * square.rank + square.file] = piece;
    }

    public void Reset()
    {
        board = new Board();
        resigned = false;
        foreach (Square s in Square.squares)
        {
            GamePiece g = Get(s);
            if (g != null) Destroy(g.gameObject);

            PieceData p = board.GetPiece(s);
            if (p != null) Spawn(p, s);
        }
        gameOverMenu.SetActive(false);
    }

    // Static only because PromoteMenu doesn't exist in the scene until the game starts
    public static void Promote(PieceType type)
    {
        bool success = instance.board.Promote(type);
        Debug.Assert(success);
        instance.UpdateScene();
    }

    public void Resign(int player)
    {
        resigned = true;
        if ((PieceColor)player == PieceColor.White)
        {
            gameOverText.text = "White resigns";
            winnerText.text = "Black wins!";
            winnerText.color = Color.black;
        }
        else
        {
            gameOverText.text = "Black resigns";
            winnerText.text = "White wins!";
            winnerText.color = Color.white;
        }
        // TODO: disable HUDs
        gameOverMenu.SetActive(true);
    }

    private void UpdateScene()
    {
        if (board.whoseTurn == PieceColor.White)
        {
            turnText.text = "White's move";
            turnText.color = Color.white;
        } else
        {
            turnText.text = "Black's move";
            turnText.color = Color.black;
        }

        // Handle castles and en passants
        foreach (Move m in board.movements)
        {
            GamePiece g = Get(m.from);
            if (m.to == m.from) {
                Spawn(board.GetPiece(m.to), m.to);
                Destroy(g.gameObject);
            }
            else // promotion
            {
                Set(m.from, null);

                if (m.to != null)
                {
                    GamePiece h = Get(m.to);
                    if (h != null) Destroy(h.gameObject);

                    Set(m.to, g);
                    g.transform.position = GetSquareCenter(m.to);
                }
                else
                {
                    Destroy(g.gameObject);
                }
            }
        }
    }

    private void Spawn(PieceData piece, Square square)
    {
        GamePiece p = Instantiate(piecePrefabs[(int)piece.type + 6 * (int)piece.color], GetSquareCenter(square), Quaternion.identity);
        Debug.Assert(p != null);
        Set(square, p);
    }

    private Square GetMouseSquare()
    {
        if (!Camera.main) return null;

        RaycastHit hit;
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 25.0f, LayerMask.GetMask("Board")))
        {
            int x = (int)Mathf.Floor((hit.point.x - boardCenter.x) / tileSize) + 4, y = (int)Mathf.Floor((hit.point.z - boardCenter.z) / tileSize) + 4;
            return (Square.Exists(x, y)) ? Square.At(x, y) : null;
        }

        return null;
    }

    private Vector3 GetSquareCenter(Square square)
    {
        return boardCenter + tileRight * (square.file - 3.5f) + tileForward * (square.rank - 3.5f);
    }

    private void DrawDebugLines()
    {
        Vector3 start = boardCorner;
        Debug.DrawLine(start, start + tileRight * 8);
        for (int i = 0; i < 8; ++i) // Horizontal lines
        {
            start += tileForward;
            Debug.DrawLine(start, start + tileRight * 8);

        }
        start = boardCorner;
        Debug.DrawLine(start, start + tileForward * 8);
        for (int j = 0; j < 8; ++j) // Vertical lines
        {
            start += tileRight;
            Debug.DrawLine(start, start + tileForward * 8);
        }

        if (mouseSquare != null) {
            Debug.DrawLine(boardCorner + tileRight * mouseSquare.file + tileForward * mouseSquare.rank,
                           boardCorner + tileRight * (mouseSquare.file + 1) + tileForward * (mouseSquare.rank + 1));

            Debug.DrawLine(boardCorner + tileRight * mouseSquare.file + tileForward * (mouseSquare.rank + 1),
                           boardCorner + tileRight * (mouseSquare.file + 1) + tileForward * mouseSquare.rank);
        }
    }
}