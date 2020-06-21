using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{
    public List<GameObject> piecePrefabs;

    // The entire game state in 4 members
    private Piece[] board;
    [NonSerialized]
    public List<Piece> hasMoved; // Contains only kings and rooks that have moved at least once
    [NonSerialized]
    public Pawn justDoubleStepped;
    private Piece.Color turn;

    private Square selectedSquare;

    private static readonly float TILE_SIZE = 1.5f;
    private static readonly Vector3 RIGHT = Vector3.right * TILE_SIZE;
    private static readonly Vector3 UP = Vector3.forward * TILE_SIZE;

    void Awake()
    {
        board = new Piece[64];
        hasMoved = new List<Piece>();
    }

    void Start()
    {
        Tests();

        Reset();
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
                    MakeMove(selectedSquare, mouseSquare);
                    if (!LegalMoves(Piece.Opponent(turn)).Any())
                    {
                        if (KingInCheck(Piece.Opponent(turn)))
                        {
                            //TODO
                            Debug.Log("CHECKMATE BITCH");
                        } else
                        {
                            Debug.Log("STALEMATE... bitch");
                        }
                    }

                    turn = Piece.Opponent(turn);
                }

                selectedSquare = null;
            }
        }
    }

    private void Put(Square square, Piece piece)
    {
        board[square.rank * 8 + square.file] = piece;
    }

    public Piece Get(Square square)
    {
        return board[square.rank * 8 + square.file];
    }

    public void MakeMove(Square from, Square to, Piece.Type promotion = Piece.Type.Pawn, bool modifyGameObjects = true)
    {
        justDoubleStepped = null;
        Piece p = Get(from);
        p.PreMove(from, to, this, promotion, modifyGameObjects); // Tells the piece to do extra things if necessary (e.g. update variables, take a pawn via en passant, move a rook via castling)
        if (Get(to) != null) Take(to, modifyGameObjects) ;
        Put(from, null);
        Put(to, p);
        if (modifyGameObjects) p.transform.position = GetTileCenter(to.file, to.rank);
    }

    public void Take(Square square, bool modifyGameObjects = true)
    {
        if (modifyGameObjects) Destroy(Get(square).gameObject);
        Put(square, null);
    }

    public void Promote(Square square, Piece.Type promotion = Piece.Type.Pawn, bool modifyGameObjects = true)
    {
        Piece p = Get(square);
        if (promotion == Piece.Type.Pawn)
        {
            //TODO: query player p.color
        }
        if (modifyGameObjects) Destroy(p.gameObject);
        Spawn(Piece.Type.Queen, p.color, square, modifyGameObjects);
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

    // Return true iff all squares are empty between FROM and TO, NONINCLUSIVE. There must be a straight-line path between FROM and TO.
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
        Debug.Assert(from != to);

        return Get(from).IsLegalMove(from, to, this) && IsSafeMove(from, to);
    }

    // True iff the king's square will not be under attack as a result of this move. Assumes FROM -> TO is otherwise a legal move.
    private bool IsSafeMove(Square from, Square to)
    {
        Piece.Color color = Get(from).color;

        // Copy the game state, perform the move, and revert.
        Piece[] boardCopy = new Piece[64];
        Array.Copy(board, boardCopy, 64);
        List<Piece> hasMovedCopy = new List<Piece>(hasMoved);
        Pawn justDoubleSteppedCopy = justDoubleStepped;

        MakeMove(from, to, Piece.Type.Knight, false); // If a promotion occurs, choose an arbitrary type to avoid querying players
        bool kingInCheck = KingInCheck(color);

        board = boardCopy;
        hasMoved = hasMovedCopy;
        justDoubleStepped = justDoubleSteppedCopy;

        return !kingInCheck;
    }

    // True iff COLOR's king is in check
    private bool KingInCheck(Piece.Color color)
    {
        Square kingSquare = null;
        Piece p = null;
        foreach (Square s in Square.squares)
        {
            p = Get(s);
            if (p != null && p.type == Piece.Type.King && p.color == color)
            {
                kingSquare = s;
                break;
            }
        }

        Debug.Assert(p != null && p.type == Piece.Type.King && p.color == color);

        return IsCheckedSquare(kingSquare, Piece.Opponent(p.color));
    }

    private struct Move
    {
        public Square from, to;

        public Move(Square _from, Square _to)
        {
            from = _from;
            to = _to;
        }
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

    private void Spawn(Piece.Type type, Piece.Color color, Square square, bool create = true)
    {
        if (create)
        {
            Quaternion rotation = (color == Piece.Color.White) ? Quaternion.identity : Quaternion.Euler(Vector3.up * 180f);
            GameObject gamePiece = Instantiate(piecePrefabs[(int)type + 6 * (int)color], GetTileCenter(square.file, square.rank), rotation) as GameObject;
            Put(square, gamePiece.GetComponent<Piece>());
        }
        else
        {
            Piece p = null;
            switch(type)
            {
                case Piece.Type.Pawn:
                    p = new Pawn();
                    break;
                case Piece.Type.Knight:
                    p = new Knight();
                    break;
                case Piece.Type.Bishop:
                    p = new Bishop();
                    break;
                case Piece.Type.Rook:
                    p = new Rook();
                    break;
                case Piece.Type.Queen:
                    p = new Queen();
                    break;
                case Piece.Type.King:
                    p = new King();
                    break;
            }

            p.color = color;
            Put(square, p);
        }
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

    private void Setup()
    {
        for (int i = 0; i < 8; ++i)
        {
            Spawn(Piece.Type.Pawn, Piece.Color.White, Square.At(i, 1));
            Spawn(Piece.Type.Pawn, Piece.Color.Black, Square.At(i, 6));
        }
        Spawn(Piece.Type.Knight, Piece.Color.White, Square.At(1, 0));
        Spawn(Piece.Type.Knight, Piece.Color.White, Square.At(6, 0));
        Spawn(Piece.Type.Knight, Piece.Color.Black, Square.At(1, 7));
        Spawn(Piece.Type.Knight, Piece.Color.Black, Square.At(6, 7));
        Spawn(Piece.Type.Bishop, Piece.Color.White, Square.At(2, 0));
        Spawn(Piece.Type.Bishop, Piece.Color.White, Square.At(5, 0));
        Spawn(Piece.Type.Bishop, Piece.Color.Black, Square.At(2, 7));
        Spawn(Piece.Type.Bishop, Piece.Color.Black, Square.At(5, 7));
        Spawn(Piece.Type.Rook, Piece.Color.White, Square.At(0, 0));
        Spawn(Piece.Type.Rook, Piece.Color.White, Square.At(7, 0));
        Spawn(Piece.Type.Rook, Piece.Color.Black, Square.At(0, 7));
        Spawn(Piece.Type.Rook, Piece.Color.Black, Square.At(7, 7));
        Spawn(Piece.Type.Queen, Piece.Color.White, Square.At(3, 0));
        Spawn(Piece.Type.Queen, Piece.Color.Black, Square.At(3, 7));
        Spawn(Piece.Type.King, Piece.Color.White, Square.At(4, 0));
        Spawn(Piece.Type.King, Piece.Color.Black, Square.At(4, 7));

        turn = Piece.Color.White;
    }

    private void Reset()
    {
        foreach (Piece p in board)
        {
            if (p != null) Destroy(p.gameObject);
        }
        board = new Piece[64];
        hasMoved = new List<Piece>();
        justDoubleStepped = null;

        Setup();
    }

    private void Tests()
    {
        Setup();

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
            Debug.Assert(Get(m.from) != null, i);
            Debug.Assert(m.from != m.to, i);
            Debug.Assert(IsLegalMove(m.from, m.to), i);
            MakeMove(m.from, m.to);
        }

        Reset();

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
            Debug.Assert(Get(m.from) != null, i);
            Debug.Assert(m.from != m.to, i);
            Debug.Assert(IsLegalMove(m.from, m.to), i);
            MakeMove(m.from, m.to);
        }
    }

    //Don't make this a struct (we want singletons with nullability - better suited as a class)
    public class Square
    {
        public readonly int file, rank;

        // Calls StraightLine(dir) in the direction of TO, stopping at TO (without returning it)
        public IEnumerable StraightLine(Square to)
        {
            int x = to.file - file, y = to.rank - rank;
            Debug.Assert(x == 0 || y == 0 || Math.Abs(x) == Math.Abs(y));

            int dir = 0;
            switch (Math.Sign(x))
            {
                case 1:
                    switch (Math.Sign(y))
                    {
                        case 1:
                            dir = 1;
                            break;
                        case -1:
                            dir = 7;
                            break;
                    }
                    break;
                case 0:
                    switch (Math.Sign(y))
                    {
                        case 1:
                            dir = 2;
                            break;
                        case -1:
                            dir = 6;
                            break;
                    }
                    break;
                case -1:
                    switch (Math.Sign(y))
                    {
                        case 1:
                            dir = 3;
                            break;
                        case 0:
                            dir = 4;
                            break;
                        case -1:
                            dir = 5;
                            break;
                    }
                    break;
            }

            foreach (Square s in StraightLine(dir))
            {
                if (s == to) yield break;
                yield return s;
            }
        }

        public IEnumerable StraightLine(int dir)
        {
            int dFile = DIRECTIONS[dir, 0], dRank = DIRECTIONS[dir, 1];
            int x = file + dFile, y = rank + dRank;
            while (Exists(x, y))
            {
                yield return At(x, y);
                x += dFile;
                y += dRank;
            }
        }

        public static IEnumerable squares { get { return Squares(); } }

        public static bool Exists(int file, int rank)
        {
            return file >= 0 && file < 8 && rank >= 0 && rank < 8;
        }

        //Rank and file must not be out of bounds
        public static Square At(int file, int rank)
        {
            return SQUARES[rank * 8 + file];
        }

        private static IEnumerable Squares()
        {
            for (int i = 0; i < 64; ++i)
            {
                yield return SQUARES[i];
            }
        }

        private Square(int index)
        {
            rank = index / 8;
            file = index % 8;
        }

        static Square()
        {
            for (int i = 63; i >= 0; --i)
            {
                SQUARES[i] = new Square(i);
            }
        }

        private static readonly Square[] SQUARES = new Square[64];

        private static readonly int[,] DIRECTIONS = { { 1, 0 }, { 1, 1 }, { 0, 1 }, { -1, 1 }, { -1, 0 }, { -1, -1 }, { 0, -1 }, { 1, -1 } };
    }
}