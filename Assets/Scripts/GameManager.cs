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
// promote menu (radial): https://answers.unity.com/questions/652859/how-can-i-make-my-gui-button-appear-above-my-click.html
// also https://answers.unity.com/questions/1107023/trouble-positioning-ui-buttons-in-radial-menu-arou.html
// outline square on mouseover (color of outline can be dependent on piece conditions, e.g. legality)
// make ghost pieces transparent
// additional Board draw conditions (threefold repetition, impossible endgame conditions, fifty moves, etc.)
// online multiplayer
// draw by mutual agreement (GameManager)
// show pieces that have been taken on the side of the board

public class GameManager : MonoBehaviour
{
    [SerializeField]
    private List<GameObject> piecePrefabs;

    [SerializeField]
    private GameObject promoteMenu, gameOverMenu;
    [SerializeField]
    private Text turnText, gameOverText, winnerText, debugText;
    //TODO: add reference to boardObject & rewrite all transform code (boardCenter returns boardObject.position, piece prefabs & ghosts are instantiated as children of boardObject, etc.)

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
                selectedPiece.transform.parent.position = GetSquareCenter(_selectedSquare); // If UpdateScene is called (on a successful move) this line won't matter anyway
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
                if (_selectedSquare != null) Get(_selectedSquare).transform.parent.position = GetSquareCenter(_mouseSquare);

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
    public static readonly Vector3 boardCenter = 4 * (tileRight + tileForward);

    private static GameManager singletonInstance = null;

    void Awake()
    {
        Debug.Assert(singletonInstance == null);
        singletonInstance = this;

        gamePieces = new GamePiece[64];

        Reset();
    }

    void Update()
    {
        if (board.status != BoardStatus.Playing || resigned) return;

        mouseSquare = GetMouseSquare();
        Draw(mouseSquare); // DEBUG

        if (Input.GetMouseButtonDown(0))
        {
            if (mouseSquare == null) // clicked outside the board
            {
                selectedSquare = null;
            }
            else if (selectedSquare == null) // clicked on the board, but no previously selected square
            {
                PieceData p = board.GetPiece(mouseSquare);
                if (p != null && p.color == board.whoseTurn)
                {
                    if (Get(mouseSquare) == null) throw new System.Exception(); // DEBUG
                    selectedSquare = mouseSquare; // only select if it's one of our pieces
                }
            }
            else // the player has attempted to make a move
            {
                bool success = board.MakeMove(new Move(selectedSquare, mouseSquare));
                selectedSquare = null;
                if (success)
                {
                    UpdateScene(); // selectedSquare must be set to null before UpdateScene()

                    switch (board.status)
                    {
                        case BoardStatus.Promote:
                            promoteMenu.SetActive(true);
                            break;
                        case BoardStatus.Checkmate:
                            gameOverText.text = "Checkmate!";
                            if (board.whoseTurn == PieceColor.White)
                            {
                                winnerText.text = "White wins!";
                                winnerText.color = Color.white;
                            } else
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
                            gameOverMenu.SetActive(true);
                            break;
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
            if (g != null) Destroy(g.transform.parent.gameObject);

            PieceData p = board.GetPiece(s);
            if (p != null) Spawn(p, s);
        }
        gameOverMenu.SetActive(false);
    }

    public void Promote(int type)
    {
        Debug.Assert(board.Promote((PieceType)type));
        UpdateScene();
        promoteMenu.SetActive(false);
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
        // TODO: this is very, very broken
        foreach (Move m in board.movements)
        {
            GamePiece g = Get(m.from);
            if (m.to != m.from) {
                Set(m.from, null);
            }
            else
            {
                Spawn(board.GetPiece(m.to), m.to);
                Destroy(g.gameObject);
            }

            if (m.to != null)
            {
                GamePiece h = Get(m.to);
                if (h != null) Destroy(h.gameObject);

                Set(m.to, g);
                g.transform.parent.position = GetSquareCenter(m.to);
            } else
            {
                Destroy(g.gameObject);
            }
        }
    }

    private void Spawn(PieceData piece, Square square)
    {
        GamePiece p = Instantiate(piecePrefabs[(int)piece.type + 6 * (int)piece.color], GetSquareCenter(square), Quaternion.identity).GetComponentInChildren<GamePiece>();
        Debug.Assert(p != null);
        Set(square, p);
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
}