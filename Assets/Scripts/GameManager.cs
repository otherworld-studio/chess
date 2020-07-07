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
// promote menu still jerks slightly when you cross the plane
// improve piece highlighting: https://forum.unity.com/threads/solved-gameobject-picking-highlighting-and-outlining.40407/
// show pieces that have been taken on the side of the board
// AI opponent
// online multiplayer

// After multiplayer:
// draw by mutual agreement (GameManager)
// additional draw conditions (see Board)

public class GameManager : MonoBehaviour
{
    [SerializeField]
    private List<GamePiece> piecePrefabs;
    [SerializeField]
    private GameObject highlightPrefab;

    [SerializeField]
    private Text turnText, gameOverText, winnerText, debugText;
    [SerializeField]
    private GameObject boardObject, gameOverMenu;

    private static GameManager instance = null; // singleton instance

    private Board board;
    private GamePiece[] gamePieces;
    private GameObject[] highlights;
    private bool resigned;

    private List<GameObject> highlighted; // TODO: assist mode?

    private Square _selectedSquare;
    private Square selectedSquare {
        get { return _selectedSquare; }
        set {
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
            if (_mouseSquare != null)
            {
                Highlight(_mouseSquare, null);
                GamePiece mousePiece = Get(_mouseSquare);
                if (mousePiece != null)
                    mousePiece.Highlight(false);
            }

            _mouseSquare = value;
            if (_mouseSquare != null)
            {
                Color color = mouseColor;

                if (_selectedSquare != null)
                {
                    Get(_selectedSquare).transform.position = GetSquareCenter(_mouseSquare);
                    if (_mouseSquare != _selectedSquare)
                    {
                        color = (board.IsLegalMove(new Move(_selectedSquare, _mouseSquare))) ? legalColor : illegalColor;

                        GamePiece mousePiece = Get(_mouseSquare);
                        if (mousePiece != null)
                            mousePiece.Highlight(true);
                    }
                }

                Highlight(_mouseSquare, color);
            }
        }
    }

    public const float tileSize = 1.5f;
    public static readonly Vector3 tileRight = Vector3.right * tileSize, tileUp = Vector3.up * tileSize, tileForward = Vector3.forward * tileSize;

    public static Vector3 boardCenter { get { return instance.boardObject.transform.position; } }
    public static Vector3 boardCorner { get { return boardCenter - 4 * (tileRight + tileForward); } }

    private const float highlightHeight = 0.01f;
    private const float highlightAlpha = 0.25f;

    private static readonly Color mouseColor = new Color(1f, 0.92f, 0.016f, highlightAlpha);
    private static readonly Color legalColor = new Color(0f, 1f, 0f, highlightAlpha);
    private static readonly Color illegalColor = new Color(1f, 0f, 0f, highlightAlpha);

    void Awake()
    {
        Debug.Assert(instance == null);
        instance = this;

        gamePieces = new GamePiece[64];
        highlights = new GameObject[64];
        foreach (Square s in Square.squares)
        {
            GameObject highlight = Instantiate(highlightPrefab, GetSquareCenter(s) + highlightHeight * tileUp, Quaternion.AngleAxis(90f, Vector3.right), boardObject.transform);
            highlight.transform.localScale = new Vector3(tileSize, tileSize, tileSize);
            highlights[8 * s.rank + s.file] = highlight;

        }
        Reset();
    }

    void Update()
    {
        if (board.status != BoardStatus.Playing || resigned)
            return;

        mouseSquare = GetMouseSquare();
        DrawDebugLines(); // DEBUG

        if (Input.GetMouseButtonDown(0))
        {
            if (mouseSquare != null)
            {
                if (selectedSquare == null) // clicked on the board, but no previously selected square
                {
                    PieceData? p = board.GetPiece(mouseSquare);
                    if (p != null && p.Value.color == board.whoseTurn)
                        selectedSquare = mouseSquare; // only select if it's one of our pieces
                }
                else if (mouseSquare == selectedSquare)
                {
                    selectedSquare = null;
                }
                else // the player has attempted to make a move
                {
                    Move move = new Move(selectedSquare, mouseSquare);
                    if (board.MakeMove(move))
                    {
                        selectedSquare = null; // piece must be unselected before anything

                        GamePiece g = Get(move.from);
                        GamePiece h = Get(move.to);
                        if (h != null)
                            Destroy(h.gameObject);
                        Set(move.from, null);
                        Set(move.to, g);
                        g.transform.position = GetSquareCenter(move.to);

                        UpdateScene((board.sideEffect != null) ? new List<Move>() { board.sideEffect.Value } : null);

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
                                // TODO: disable HUDs
                                gameOverMenu.SetActive(true);
                                break;
                            case BoardStatus.Stalemate:
                                gameOverText.text = "Stalemate!";
                                winnerText.text = "Draw";
                                winnerText.color = (board.whoseTurn == PieceColor.White) ? Color.white : Color.black;
                                // TODO: disable HUDs
                                gameOverMenu.SetActive(true);
                                break;
                            case BoardStatus.InsufficientMaterial:
                                gameOverText.text = "Insufficient material to force a checkmate!";
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
            if (g != null)
                Destroy(g.gameObject);

            PieceData? p = board.GetPiece(s);
            if (p != null)
                Spawn(p.Value, s);
        }
        gameOverMenu.SetActive(false);
    }

    // Static only because PromoteMenu doesn't exist in the scene until the game starts
    public static void Promote(PieceType type)
    {
        Square needsPromotion = instance.board.needsPromotion;
        bool success = instance.board.Promote(type);
        Debug.Assert(success);

        Destroy(instance.Get(needsPromotion).gameObject);
        instance.Spawn(instance.board.GetPiece(needsPromotion).Value, needsPromotion);

        instance.UpdateScene(null);
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

    private void UpdateScene(List<Move> updates)
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

        // For handling en passants, castles, and AI moves
        if (updates != null)
        {
            foreach (Move m in updates)
            {
                GamePiece g = Get(m.from);
                Set(m.from, null);
                if (m.to == null) // en passant
                {
                    Destroy(g.gameObject);
                }
                else
                {
                    GamePiece h = Get(m.to);
                    if (h != null)
                        Destroy(h.gameObject);
                    Set(m.to, g);

                    g.Move(GetSquareCenter(m.from), GetSquareCenter(m.to));

                    // TODO: handle AI automatic promotions
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

    private void Highlight(Square square, Color? color)
    {
        GameObject highlight = highlights[8 * square.rank + square.file];
        if (color == null)
        {
            highlight.SetActive(false);
        } else
        {
            highlight.GetComponent<Renderer>().material.color = color.Value;
            highlight.SetActive(true);
        }
    }

    private Square GetMouseSquare()
    {
        if (!Camera.main)
            return null;

        RaycastHit hit;
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, Mathf.Infinity, LayerMask.GetMask("Board")))
        {
            int x = (int)Mathf.Floor((hit.point.x - boardCenter.x) / tileSize) + 4, y = (int)Mathf.Floor((hit.point.z - boardCenter.z) / tileSize) + 4;
            return (Square.Exists(x, y)) ? Square.At(x, y) : null;
        }

        return null;
    }

    /*
    private Vector3 GetSquareCenter(Square square, bool local = false)
    {
        return (local) ? tileRight * (square.file + 0.5f) + tileForward * (square.rank + 0.5f) : boardCenter + tileRight * (square.file - 3.5f) + tileForward * (square.rank - 3.5f);
    }
    */

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