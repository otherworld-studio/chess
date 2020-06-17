using System;
using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{
    public List<GameObject> piecePrefabs;

    private Piece[] board;
    [NonSerialized]
    public Pawn justDoubleStepped;

    private Piece.Color turn;
    private Square selectedSquare;

    private const float TILE_SIZE = 1.0f;
    private static readonly Vector3 right = Vector3.right * TILE_SIZE;
    private static readonly Vector3 up = Vector3.forward * TILE_SIZE;

    void Awake()
    {
        board = new Piece[64];
    }

    void Start()
    {
        for (int i = 0; i < 8; ++i)
        {
            SpawnPiece(Piece.Type.Pawn, Piece.Color.White, Square.At(i, 1));
            SpawnPiece(Piece.Type.Pawn, Piece.Color.Black, Square.At(i, 6));
        }
        SpawnPiece(Piece.Type.Knight, Piece.Color.White, Square.At(1, 0));
        SpawnPiece(Piece.Type.Knight, Piece.Color.White, Square.At(6, 0));
        SpawnPiece(Piece.Type.Knight, Piece.Color.Black, Square.At(1, 7));
        SpawnPiece(Piece.Type.Knight, Piece.Color.Black, Square.At(6, 7));
        SpawnPiece(Piece.Type.Bishop, Piece.Color.White, Square.At(2, 0));
        SpawnPiece(Piece.Type.Bishop, Piece.Color.White, Square.At(5, 0));
        SpawnPiece(Piece.Type.Bishop, Piece.Color.Black, Square.At(2, 7));
        SpawnPiece(Piece.Type.Bishop, Piece.Color.Black, Square.At(5, 7));
        SpawnPiece(Piece.Type.Rook, Piece.Color.White, Square.At(0, 0));
        SpawnPiece(Piece.Type.Rook, Piece.Color.White, Square.At(7, 0));
        SpawnPiece(Piece.Type.Rook, Piece.Color.Black, Square.At(0, 7));
        SpawnPiece(Piece.Type.Rook, Piece.Color.Black, Square.At(7, 7));
        SpawnPiece(Piece.Type.Queen, Piece.Color.White, Square.At(3, 0));
        SpawnPiece(Piece.Type.Queen, Piece.Color.Black, Square.At(3, 7));
        SpawnPiece(Piece.Type.King, Piece.Color.White, Square.At(4, 0));
        SpawnPiece(Piece.Type.King, Piece.Color.Black, Square.At(4, 7));

        turn = Piece.Color.White;
    }

    void Update()
    {
        Square mouseSquare = GetMouseSquare();
        Draw(mouseSquare);//DEBUG

        if (Input.GetMouseButtonDown(0) && mouseSquare != null)
        {
            Piece p = Get(mouseSquare);
            if (p != null && p.color == turn)
            {
                selectedSquare = mouseSquare;
            }
            else if (selectedSquare != null)
            {
                if (IsLegalMove(selectedSquare, mouseSquare)) {
                    justDoubleStepped = null;
                    Move(selectedSquare, mouseSquare);
                    if (Checkmate(Piece.Opponent(turn)))
                    {
                        //TODO
                    }
                    turn = Piece.Opponent(turn);
                }
                selectedSquare = null;
            }
        }
    }

    public Piece Get(Square square)
    {
        return board[square.rank * 8 + square.file];
    }

    private void Set(Square square, Piece piece)
    {
        board[square.rank * 8 + square.file] = piece;
    }

    public void Move(Square from, Square to)
    {
        Piece p = Get(from);
        p.PreMove(from, to, this); // Tells the piece to do extra things if necessary (e.g. update variables, take a pawn via en passant, move a rook via castling)
        if (Get(to) != null) Take(to);
        Set(from, null);
        Set(to, p);
        p.transform.position = GetTileCenter(to.file, to.rank);
    }

    public void Take(Square square)
    {
        Destroy(Get(square).gameObject);
        Set(square, null);
    }

    public void Promote(Square square)
    {
        Destroy(Get(square).gameObject);
        Set(square, null);
        SpawnPiece(Piece.Type.Queen, turn, square);
        //TODO: choose new piece (queen, knight, bishop, rook)
    }

    // COLOR is the opponent's color
    // Q: Can I move my king to this square? A:
    public bool IsChecked(Square square, Piece.Color color)
    {
        foreach (Square s in Square.Iterator())
        {
            Piece p = Get(s);
            if (p != null && p.color == color && p.IsChecking(s, square, this)) return true;
        }

        return false;
    }

    private bool IsLegalMove(Square from, Square to)
    {
        if (from == to) return false;

        Piece p = Get(from);
        if (p == null || p.color != turn || !p.IsLegalMove(from, to, this)) return false;

        // In order to determine whether the king will be in check, we copy the game state, perform the move, and reset.
        Piece[] boardCopy = new Piece[64];
        Array.Copy(board, boardCopy, 64);
        Pawn justDoubleSteppedCopy = justDoubleStepped;

        p.PreMove(from, to, this);
        Set(from, null);
        Set(to, p);

        bool kingInCheck = false;
        foreach (Square s in Square.Iterator())
        {
            p = Get(s);
            if (p != null && p.type == Piece.Type.King && p.color == turn)
            {
                kingInCheck = IsChecked(s, Piece.Opponent(turn));
                break;
            }
        }

        Array.Copy(boardCopy, board, 64);
        justDoubleStepped = justDoubleSteppedCopy;

        return !kingInCheck;
    }

    // Return true iff all squares are empty between FROM and TO, NONINCLUSIVE.
    public bool IsUnblockedPath(Square from, Square to)
    {
        int x = to.file - from.file, y = to.rank - from.rank;
        if (x != 0 && y != 0 && Math.Abs(x) != Math.Abs(y)) return false;

        int dFile = Math.Sign(x), dRank = Math.Sign(y);
        Square s = Square.At(from.file + dFile, from.rank + dRank);
        do
        {
            if (Get(s) != null) return false;
            s = Square.At(s.file + dFile, s.rank + dRank);
        } while (s != to);

        return true;
    }

    // True iff COLOR's king is in checkmate
    public bool Checkmate(Piece.Color color)
    {
        //TODO: iterators
    }

    private Square GetMouseSquare()
    {
        if (!Camera.main) return null;

        RaycastHit hit;
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 25.0f, LayerMask.GetMask("Board")))
        {
            int x = (int)(hit.point.x / TILE_SIZE), y = (int)(hit.point.z / TILE_SIZE);
            return (Square.Exists(x, y)) ? Square.At(x, y) : null;
        }

        return null;
    }

    private void SpawnPiece(Piece.Type type, Piece.Color color, Square square)
    {
        Debug.Log("Spawning " + color + " " + type + " at x = " + square.file + ", y = " + square.rank);
        Quaternion rotation = (color == Piece.Color.White) ? Quaternion.identity : Quaternion.Euler(Vector3.up * 180f);
        GameObject gamePiece = Instantiate(piecePrefabs[(int)type + 6 * (int)color], GetTileCenter(square.file, square.rank), Quaternion.identity) as GameObject;
        Set(square, gamePiece.GetComponent<Piece>());
    }

    private Vector3 GetTileCenter(int file, int rank)
    {
        return right * (file + 0.5f) + up * (rank + 0.5f);
    }

    private void Draw(Square mouseSquare)
    {
        Vector3 widthLine = right * 8;
        Vector3 depthLine = up * 8;
        for (int i = 0; i <= 8; ++i)
        {
            Vector3 start = up * i;
            Debug.DrawLine(start, start + widthLine);

        }
        for (int j = 0; j <= 8; ++j)
        {
            Vector3 start = right * j;
            Debug.DrawLine(start, start + depthLine);
        }

        if (mouseSquare != null) {
            Debug.DrawLine(right * mouseSquare.file + up * mouseSquare.rank,
                           right * (mouseSquare.file + 1) + up * (mouseSquare.rank + 1));

            Debug.DrawLine(right * mouseSquare.file + up * (mouseSquare.rank + 1),
                           right * (mouseSquare.file + 1) + up * mouseSquare.rank);
        }
    }
}