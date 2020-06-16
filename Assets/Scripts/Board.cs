using System;
using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{
    public List<GameObject> piecePrefabs;

    private Piece[] board;
    
    private const float TILE_SIZE = 1.0f;
    private static readonly Vector3 right = Vector3.right * TILE_SIZE;
    private static readonly Vector3 up = Vector3.forward * TILE_SIZE;

    private Piece.Color turn;
    private Square selectedSquare;

    [NonSerialized]
    public Pawn justDoubleStepped;

    void Start()
    {
        Setup();
    }

    void Update()
    {
        Square mouseSquare = GetMouseSquare();
        Draw(mouseSquare);//DEBUG

        if (Input.GetMouseButtonDown(0) && mouseSquare != null)
        {
            Piece piece = Get(mouseSquare);
            if (piece != null && piece.color == turn)
            {
                selectedSquare = mouseSquare;
            }
            else if (selectedSquare != null)
            {
                piece = Get(selectedSquare);
                if (IsLegalMove(selectedSquare, mouseSquare)) {
                    justDoubleStepped = null;
                    Move(selectedSquare, mouseSquare);
                    //Look for check/checkmate
                }
                selectedSquare = null;
            }
        }
    }

    public Piece Get(int file, int rank)
    {
        if (!Square.Exists(file, rank)) return null;

        return board[rank * 8 + file];
    }

    public Piece Get(Square square)
    {
        return board[square.rank * 8 + square.file];
    }

    private void Set(Square square, Piece piece)
    {
        board[square.rank * 8 + square.file] = piece;
    }

    private void Clear(Square square)
    {
        Destroy(Get(square).gameObject);
        Set(square, null);
    }
    
    private void Move(Square from, Square to)
    {
        Piece piece = Get(from);
        piece.transform.position = GetTileCenter(to.file, to.rank);
        Set(from, null);
        Set(to, piece);
        piece.Move(from, to, this);//Tells the piece to update variables if necessary (e.g. pawn.justDoubleStepped, rook.hasMoved, king.hasMoved)
        turn = Piece.Opponent(turn);
    }

    public void Promote(Pawn p)
    {
        //TODO
    }

    private bool IsLegalMove(Square from, Square to)
    {
        if (from == to) return false;

        Piece p = Get(from);
        if (p == null || p.color != turn) return false;

        return p.IsLegalMove(from, to, this) && TODO_KING_CHECK?;
    }

    // If toAsEmpty, return true iff all squares between from and to (NONINCLUSIVE) are empty.
    // If !toAsEmpty, return true iff all squares between from and to, INCLUDING to, are empty
    public bool IsUnblockedPath(Square from, Square to, bool toAsEmpty)
    {
        int x = to.file - from.file;
        int y = to.rank - from.rank;
        if (x != 0 && y != 0 && Math.Abs(x) != Math.Abs(y)) return false;

        int dFile = Math.Sign(x), dRank = Math.Sign(y);
        x = from.file + dFile;
        y = from.rank + dRank;
        do
        {
            if (Get(x, y) != null) return false;
            x += dFile;
            y += dRank;
        } while (x != to.file);

        return toAsEmpty || Get(to) == null;
    }

    private Square GetMouseSquare()
    {
        if (!Camera.main) return null; // TODO: NECESSARY?

        RaycastHit hit;
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 25.0f, LayerMask.GetMask("Board")))
        {
            int x = (int)(hit.point.x / TILE_SIZE), y = (int)(hit.point.z / TILE_SIZE);
            return (Square.Exists(x, y)) ? Square.Get(x, y) : null;
        }

        return null;
    }

    private void SpawnPiece(Piece.Type type, Piece.Color color, Square square)
    {
        Debug.Log("Spawning " + color + " " + type + " at x = " + square.file + ", y = " + square.rank);
        Quaternion rotation = (color == Piece.Color.White) ? Quaternion.identity : Quaternion.Euler(Vector3.up * 180);
        GameObject gamePiece = Instantiate(piecePrefabs[(int)type + 6 * (int)color], GetTileCenter(square.file, square.rank), Quaternion.identity) as GameObject;
        Set(square, gamePiece.GetComponent<Piece>());
    }

    private Vector3 GetTileCenter(int file, int rank)
    {
        return right * (file + 0.5f) + up * (rank + 0.5f);
    }

    private void Setup()
    {
        board = new Piece[64];
        for (int i = 0; i < 8; ++i)
        {
            SpawnPiece(Piece.Type.Pawn, Piece.Color.White, Square.Get(i, 1));
            SpawnPiece(Piece.Type.Pawn, Piece.Color.Black, Square.Get(i, 6));
        }
        SpawnPiece(Piece.Type.Knight, Piece.Color.White, Square.Get(1, 0));
        SpawnPiece(Piece.Type.Knight, Piece.Color.White, Square.Get(6, 0));
        SpawnPiece(Piece.Type.Knight, Piece.Color.Black, Square.Get(1, 7));
        SpawnPiece(Piece.Type.Knight, Piece.Color.Black, Square.Get(6, 7));
        SpawnPiece(Piece.Type.Bishop, Piece.Color.White, Square.Get(2, 0));
        SpawnPiece(Piece.Type.Bishop, Piece.Color.White, Square.Get(5, 0));
        SpawnPiece(Piece.Type.Bishop, Piece.Color.Black, Square.Get(2, 7));
        SpawnPiece(Piece.Type.Bishop, Piece.Color.Black, Square.Get(5, 7));
        SpawnPiece(Piece.Type.Rook, Piece.Color.White, Square.Get(0, 0));
        SpawnPiece(Piece.Type.Rook, Piece.Color.White, Square.Get(7, 0));
        SpawnPiece(Piece.Type.Rook, Piece.Color.Black, Square.Get(0, 7));
        SpawnPiece(Piece.Type.Rook, Piece.Color.Black, Square.Get(7, 7));
        SpawnPiece(Piece.Type.Queen, Piece.Color.White, Square.Get(3, 0));
        SpawnPiece(Piece.Type.Queen, Piece.Color.Black, Square.Get(3, 7));
        SpawnPiece(Piece.Type.King, Piece.Color.White, Square.Get(4, 0));
        SpawnPiece(Piece.Type.King, Piece.Color.Black, Square.Get(4, 7));

        turn = Piece.Color.White;
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