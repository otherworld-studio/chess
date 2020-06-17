using System;
using System.Linq;
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
                        Debug.Log("CHECKMATE BITCHES");
                    }
                    turn = Piece.Opponent(turn);
                }
                selectedSquare = null;
            }
        }
    }

    private void Set(Square square, Piece piece)
    {
        board[square.rank * 8 + square.file] = piece;
    }

    public Piece Get(Square square)
    {
        return board[square.rank * 8 + square.file];
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
        //TODO: allow player to choose new piece from (queen, knight, bishop, rook)
    }

    // Q: Could I move my king to this square? A:
    public bool IsCheckedSquare(Square square, Piece.Color color) // COLOR is the opponent's color
    {
        foreach (Square s in Square.Iterator())
        {
            Piece p = Get(s);
            if (p != null && p.color == color && p.IsCheckedSquare(square, s, this)) return true;
        }

        return false;
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

    private bool IsLegalMove(Square from, Square to)
    {
        Debug.Assert(from != to);

        Piece p = Get(from);
        Debug.Assert(p != null);
        if (!p.IsLegalMove(from, to, this)) return false;

        // In order to determine whether the king will be in check, we copy the game state, perform the move, and revert.
        Piece[] boardCopy = new Piece[64];
        Array.Copy(board, boardCopy, 64);
        Pawn justDoubleSteppedCopy = justDoubleStepped;

        p.PreMove(from, to, this);
        Set(from, null);
        Set(to, p);

        bool kingInCheck = false;
        foreach (Square s in Square.Iterator())
        {
            Piece q = Get(s);
            if (q != null && q.type == Piece.Type.King && q.color == p.color)
            {
                kingInCheck = IsCheckedSquare(s, Piece.Opponent(q.color));
                break;
            }
        }

        Array.Copy(boardCopy, board, 64);
        justDoubleStepped = justDoubleSteppedCopy;
        //TODO: revert hasMoved for each rook/king

        return !kingInCheck;
    }

    // True iff COLOR's king is in checkmate
    private bool Checkmate(Piece.Color color)
    {
        //return !LegalMovesIterator(color).Any();
        return false;
    }

    /*
    private IEnumerable<Move> LegalMovesIterator(Piece.Color color)
    {
        // TODO: make a legal move iterator for each piece that takes as an argument its current position and the board
        // Then iterate over the iterators of all pieces of this color, yielding only legal moves
        // Don't call IsLegalMove at all - this will in turn call piece.IsLegalMove() which is unnecessary
        // Instead perform all other steps of IsLegalMove
    }
    */

    private void SpawnPiece(Piece.Type type, Piece.Color color, Square square)
    {
        Debug.Log("Spawning " + color + " " + type + " at x = " + square.file + ", y = " + square.rank);
        Quaternion rotation = (color == Piece.Color.White) ? Quaternion.identity : Quaternion.Euler(Vector3.up * 180f);
        GameObject gamePiece = Instantiate(piecePrefabs[(int)type + 6 * (int)color], GetTileCenter(square.file, square.rank), Quaternion.identity) as GameObject;
        Set(square, gamePiece.GetComponent<Piece>());
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