using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{
    public List<GameObject> piecePrefabs;

    private Piece[] board;
    [NonSerialized]
    public List<Piece> hasMoved; // Contains only kings and rooks that have moved at least once
    [NonSerialized]
    public Pawn justDoubleStepped;

    private Piece.Color turn;
    private Square selectedSquare;

    private static readonly float TILE_SIZE = 1.0f;
    private static readonly Vector3 RIGHT = Vector3.right * TILE_SIZE;
    private static readonly Vector3 UP = Vector3.forward * TILE_SIZE;

    void Awake()
    {
        board = new Piece[64];
        hasMoved = new List<Piece>();
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
        Piece p = Get(square);
        Destroy(p.gameObject);
        Set(square, null);
        SpawnPiece(Piece.Type.Queen, p.color, square);
        //TODO: allow player to choose new piece from (queen, knight, bishop, rook)
    }

    // Q: Could I move my king to this square? A:
    public bool IsCheckedSquare(Square square, Piece.Color color) // COLOR is the opponent's color
    {
        foreach (Square s in Square.squares)
        {
            Piece p = Get(s);
            if (p != null && p.color == color && p.IsCheckedSquare(square, s, this)) return true;
        }

        return false;
    }

    // Return true iff all squares are empty between FROM and TO, NONINCLUSIVE.
    public bool IsUnblockedPath(Square from, Square to)
    {
        foreach (Square s in from.StraightLine(to))
        {
            if (Get(s) != null) return false;
        }

        return true;
    }

    private bool IsLegalMove(Square from, Square to)
    {
        Debug.Assert(from != to); // DEBUG

        return Get(from).IsLegalMove(from, to, this) && IsSafeMove(from, to);
    }

    // True iff the king's square will not be under attack as a result of this move.
    private bool IsSafeMove(Square from, Square to)
    {
        Piece p = Get(from);

        // Copy the game state, perform the move, and revert.
        Piece[] boardCopy = new Piece[64];
        Array.Copy(board, boardCopy, 64);
        List<Piece> hasMovedCopy = new List<Piece>(hasMoved);
        Pawn justDoubleSteppedCopy = justDoubleStepped;

        p.PreMove(from, to, this);
        Set(from, null);
        Set(to, p);

        bool kingInCheck = KingInCheck(p.color);

        board = boardCopy;
        hasMoved = hasMovedCopy;
        justDoubleStepped = justDoubleSteppedCopy;

        return !kingInCheck;
    }

    // True iff COLOR's king is in check
    private bool KingInCheck(Piece.Color color)
    {
        foreach (Square s in Square.squares)
        {
            Piece q = Get(s);
            if (q != null && q.type == Piece.Type.King && q.color == color)
            {
                return IsCheckedSquare(s, Piece.Opponent(q.color));
            }
        }

        throw new Exception("no king found"); // DEBUG
    }

    // True iff COLOR's king is in checkmate
    private bool Checkmate(Piece.Color color)
    {
        return !LegalMoves(color).Any();
    }

    private IEnumerable<Move> LegalMoves(Piece.Color color)
    {
        foreach (Square from in Square.squares)
        {
            Piece p = Get(from);
            if (p == null || p.color != color) continue;

            foreach (Square to in p.LegalMoves(from, this))
            {
                if (IsSafeMove(from, to)) yield return new Move(from, to);
            }
        }
    }

    private void SpawnPiece(Piece.Type type, Piece.Color color, Square square)
    {
        Debug.Log("Spawning " + color + " " + type + " at x = " + square.file + ", y = " + square.rank);
        Quaternion rotation = (color == Piece.Color.White) ? Quaternion.identity : Quaternion.Euler(Vector3.up * 180f);
        GameObject gamePiece = Instantiate(piecePrefabs[(int)type + 6 * (int)color], GetTileCenter(square.file, square.rank), rotation) as GameObject;
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
        return RIGHT * (file + 0.5f) + UP * (rank + 0.5f);
    }

    private void Draw(Square mouseSquare)
    {
        Vector3 widthLine = RIGHT * 8;
        Vector3 depthLine = UP * 8;
        for (int i = 0; i <= 8; ++i)
        {
            Vector3 start = UP * i;
            Debug.DrawLine(start, start + widthLine);

        }
        for (int j = 0; j <= 8; ++j)
        {
            Vector3 start = RIGHT * j;
            Debug.DrawLine(start, start + depthLine);
        }

        if (mouseSquare != null) {
            Debug.DrawLine(RIGHT * mouseSquare.file + UP * mouseSquare.rank,
                           RIGHT * (mouseSquare.file + 1) + UP * (mouseSquare.rank + 1));

            Debug.DrawLine(RIGHT * mouseSquare.file + UP * (mouseSquare.rank + 1),
                           RIGHT * (mouseSquare.file + 1) + UP * mouseSquare.rank);
        }
    }
}