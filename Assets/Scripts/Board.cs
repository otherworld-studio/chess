using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Board
{
    // The entire game state in 4 members
    private Piece[] board;
    private HashSet<Piece> hasMoved; // Contains only kings and rooks that have moved at least once
    private Pawn justDoubleStepped;
    public PieceColor turn { get; private set; }

    public BoardStatus status { get; private set; }

    public enum BoardStatus
    {
        Playing, // waiting on a player to make a move
        Promote, // waiting on a player to choose a new piece to replace a pawn that has reached the end of the board
        Checkmate, // self-explanatory
        Stalemate // etc.
    }

    private Square needsPromotion;

    public Board()
    {
        Reset();
    }

    public PieceTag GetPiece(Square square)
    {
        Piece p = Get(square);
        return p?.tag;
    }

    private void Put(Square square, Piece piece)
    {
        board[square.rank * 8 + square.file] = piece;
    }

    private Piece Get(Square square)
    {
        return board[square.rank * 8 + square.file];
    }

    // Returns true iff the move is made successfully
    public bool MakeMove(Square from, Square to, PieceType promotion = PieceType.Queen)
    {
        if (!IsLegalMove(from, to, promotion)) return false;

        justDoubleStepped = null;

        Piece p = Get(from);
        p.PreMove(from, to, this, promotion); // Tells the piece to do extra things if necessary (e.g. update variables, take a pawn via en passant, move a rook via castling)
        Put(from, null);
        Put(to, p);

        if (needsPromotion != null)
        {
            status = BoardStatus.Promote;
        } else
        {
            turn = Opponent(turn);
            if (!legalMoves.Any())
            {
                if (KingInCheck())
                {
                    status = BoardStatus.Checkmate;
                }
                else
                {
                    status = BoardStatus.Stalemate;
                }
            }
        }

        return true;
    }

    public bool Promote(PieceType type)
    {
        if (status != BoardStatus.Promote || type == PieceType.Pawn || type == PieceType.King) return false;

        Piece.Spawn(type, Get(needsPromotion).color, needsPromotion, this);

        needsPromotion = null;

        turn = Opponent(turn);
        if (!legalMoves.Any())
        {
            if (KingInCheck())
            {
                status = BoardStatus.Checkmate;
            }
            else
            {
                status = BoardStatus.Stalemate;
            }
        }
        else
        {
            status = BoardStatus.Playing;
        }

        return true;
    }

    public bool IsLegalMove(Square from, Square to, PieceType promotion = PieceType.Pawn)
    {
        if (status != BoardStatus.Playing || from == to) return false;

        Piece p = Get(from);
        if (p.color != turn) return false;

        return p.IsLegalMove(from, to, this, promotion) && IsSafeMove(from, to);
    }

    // True iff TURN's king's square will not be under attack as a result of this move. Assumes FROM -> TO is otherwise a legal move.
    private bool IsSafeMove(Square from, Square to)
    {
        // Copy the game state, perform the move, and revert.
        Piece[] boardCopy = new Piece[64];
        Array.Copy(board, boardCopy, 64);
        HashSet<Piece> hasMovedCopy = new HashSet<Piece>(hasMoved);
        Pawn justDoubleSteppedCopy = justDoubleStepped;
        
        Piece p = Get(from);
        p.PreMove(from, to, this); // Tells the piece to do extra things if necessary (e.g. update variables, take a pawn via en passant, move a rook via castling)
        Put(from, null);
        Put(to, p);
        bool kingInCheck = KingInCheck();

        board = boardCopy;
        hasMoved = hasMovedCopy;
        justDoubleStepped = justDoubleSteppedCopy;

        return !kingInCheck;
    }

    // True iff TURN's king is threatened by an opponent's piece
    public bool KingInCheck()
    {
        Square kingSquare = null;
        Piece p = null;
        foreach (Square s in Square.squares)
        {
            p = Get(s);
            if (p != null && p.type == PieceType.King && p.color == turn)
            {
                kingSquare = s;
                break;
            }
        }

        Debug.Assert(p != null && p.type == PieceType.King && p.color == turn);

        return IsCheckedSquare(kingSquare, Opponent(turn));
    }

    // True iff COLOR's opponent can move their king to this SQUARE.
    public bool IsCheckedSquare(Square square, PieceColor color)
    {
        foreach (Square s in Square.squares)
        {
            Piece p = Get(s);
            if (p != null && p.color == color && p.IsCheckedSquare(square, s, this)) return true;
        }

        return false;
    }

    // True iff all squares are empty between FROM and TO, NONINCLUSIVE. There must be a straight-line path between FROM and TO.
    public bool IsUnblockedPath(Square from, Square to)
    {
        foreach (Square s in from.StraightLine(to))
        {
            if (Get(s) != null) return false;
        }

        return true;
    }

    public struct Move
    {
        public Square from, to;

        public Move(Square _from, Square _to)
        {
            from = _from;
            to = _to;
        }
    }

    public IEnumerable<Move> legalMoves
    {
        get
        {
            foreach (Square from in Square.squares)
            {
                Piece p = Get(from);
                if (p == null || p.color != turn) continue;

                foreach (Square to in p.LegalMoves(from, this))
                {
                    if (IsSafeMove(from, to)) yield return new Move(from, to);
                }
            }
        }
    }

    public void Reset()
    {
        board = new Piece[64];
        hasMoved = new HashSet<Piece>();
        justDoubleStepped = null;
        turn = PieceColor.White;

        for (int i = 0; i < 8; ++i)
        {
            Piece.Spawn(PieceType.Pawn, PieceColor.White, Square.At(i, 1), this);
            Piece.Spawn(PieceType.Pawn, PieceColor.Black, Square.At(i, 6), this);
        }
        Piece.Spawn(PieceType.Knight, PieceColor.White, Square.At(1, 0), this);
        Piece.Spawn(PieceType.Knight, PieceColor.White, Square.At(6, 0), this);
        Piece.Spawn(PieceType.Knight, PieceColor.Black, Square.At(1, 7), this);
        Piece.Spawn(PieceType.Knight, PieceColor.Black, Square.At(6, 7), this);
        Piece.Spawn(PieceType.Bishop, PieceColor.White, Square.At(2, 0), this);
        Piece.Spawn(PieceType.Bishop, PieceColor.White, Square.At(5, 0), this);
        Piece.Spawn(PieceType.Bishop, PieceColor.Black, Square.At(2, 7), this);
        Piece.Spawn(PieceType.Bishop, PieceColor.Black, Square.At(5, 7), this);
        Piece.Spawn(PieceType.Rook, PieceColor.White, Square.At(0, 0), this);
        Piece.Spawn(PieceType.Rook, PieceColor.White, Square.At(7, 0), this);
        Piece.Spawn(PieceType.Rook, PieceColor.Black, Square.At(0, 7), this);
        Piece.Spawn(PieceType.Rook, PieceColor.Black, Square.At(7, 7), this);
        Piece.Spawn(PieceType.Queen, PieceColor.White, Square.At(3, 0), this);
        Piece.Spawn(PieceType.Queen, PieceColor.Black, Square.At(3, 7), this);
        Piece.Spawn(PieceType.King, PieceColor.White, Square.At(4, 0), this);
        Piece.Spawn(PieceType.King, PieceColor.Black, Square.At(4, 7), this);
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
            int dfile = DIRECTIONS[dir, 0], drank = DIRECTIONS[dir, 1];
            int x = file + dfile, y = rank + drank;
            while (Exists(x, y))
            {
                yield return At(x, y);
                x += dfile;
                y += drank;
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

    public enum PieceType
    {
        Pawn,
        Knight,
        Bishop,
        Rook,
        Queen,
        King
    }

    public enum PieceColor
    {
        White,
        Black
    }

    public static PieceColor Opponent(PieceColor color)
    {
        return (color == PieceColor.White) ? PieceColor.Black : PieceColor.White;
    }

    public class PieceTag
    {
        public readonly PieceType type;
        public readonly PieceColor color;

        public PieceTag(PieceType _type, PieceColor _color)
        {
            type = _type;
            color = _color;
        }
    }

    private abstract class Piece
    {
        public readonly PieceTag tag;

        public PieceType type { get { return tag.type;  } }
        public PieceColor color { get { return tag.color;  } }

        public PieceColor opponent { get { return Opponent(color); } }

        public Piece(PieceType _type, PieceColor _color)
        {
            tag = new PieceTag(_type, _color);
        }

        // True iff this piece can make this move on this BOARD. Assume that FROM != TO and that it is this color's turn. Doesn't care if the king is in check or is put into a checked square as a result of this move.
        public abstract bool IsLegalMove(Square from, Square to, Board board, PieceType promotion = PieceType.Pawn);
        // True iff a king located at TARGET would be in check due to this piece at its CURRENT position, on the current BOARD. Assume TARGET != CURRENT.
        public abstract bool IsCheckedSquare(Square target, Square current, Board board);

        public abstract IEnumerable<Square> LegalMoves(Square from, Board board);

        // Assumes the move FROM -> TO is legal, and that promotion != PieceType.King
        public virtual void PreMove(Square from, Square to, Board board, PieceType promotion = PieceType.Pawn) { return; }

        public static void Spawn(PieceType type, PieceColor color, Square square, Board board)
        {
            Piece p = null;
            switch(type)
            {
                case PieceType.Pawn:
                    p = new Pawn(color);
                    break;
                case PieceType.Knight:
                    p = new Knight(color);
                    break;
                case PieceType.Bishop:
                    p = new Bishop(color);
                    break;
                case PieceType.Rook:
                    p = new Rook(color);
                    break;
                case PieceType.Queen:
                    p = new Queen(color);
                    break;
                case PieceType.King:
                    p = new King(color);
                    break;
            }
            board.Put(square, p);
        }
    }

    private class Pawn : Piece
    {
        public Pawn(PieceColor _color) : base(PieceType.Pawn, _color) { }

        public override bool IsLegalMove(Square from, Square to, Board board, PieceType promotion = PieceType.Pawn)
        {
            if (promotion == PieceType.King) return false;

            int steps = (color == PieceColor.White) ? to.rank - from.rank : from.rank - to.rank;
            if (from.file == to.file)
            {
                return board.Get(to) == null && (steps == 1 || steps == 2 && from.rank == ((color == PieceColor.White) ? 1 : 6) && board.IsUnblockedPath(from, to));
            }
            if (steps == 1 && Math.Abs(to.file - from.file) == 1)
            {
                Piece p = board.Get(to);
                if (p != null) return p.color == opponent;

                // En passant
                p = board.Get(Square.At(to.file, from.rank));
                return p != null && p == board.justDoubleStepped;
            }

            return false;
        }

        public override bool IsCheckedSquare(Square target, Square current, Board board)
        {
            int steps = (color == PieceColor.White) ? target.rank - current.rank : current.rank - target.rank;
            return steps == 1 && Math.Abs(target.file - current.file) == 1;
        }

        public override IEnumerable<Square> LegalMoves(Square from, Board board)
        {
            int y = (color == PieceColor.White) ? from.rank + 1 : from.rank - 1;
            if (!Square.Exists(from.file, y)) yield break;

            Square s = Square.At(from.file, y);
            if (board.Get(s) == null)
            {
                yield return s;

                if (from.rank == ((color == PieceColor.White) ? 1 : 6))
                {
                    s = Square.At(from.file, (color == PieceColor.White) ? 3 : 4);
                    if (board.Get(s) == null)
                    {
                        yield return s;
                    }
                }
            }

            int[] files = { from.file - 1, from.file + 1 };
            foreach (int x in files)
            {
                if (Square.Exists(x, y))
                {
                    s = Square.At(x, y);
                    Piece p = board.Get(s);
                    if (p != null)
                    {
                        if (p.color == opponent) yield return s;
                    }
                    else
                    {
                        p = board.Get(Square.At(x, from.rank));
                        if (p != null && p == board.justDoubleStepped) yield return s;
                    }
                }
            }
        }

        public override void PreMove(Square from, Square to, Board board, PieceType promotion = PieceType.Pawn)
        {
            if (Math.Abs(to.rank - from.rank) == 2)
            {
                board.justDoubleStepped = this;
            }
            else if (from.file != to.file && board.Get(to) == null)
            {
                board.Put(Square.At(to.file, from.rank), null); // En passant
            }
            else if (to.rank == 7 || to.rank == 0)
            {
                if (promotion == PieceType.Pawn)
                {
                    board.needsPromotion = to; // Delay piece selection
                } else
                {
                    Spawn(promotion, color, from, board);
                }
                
            }
        }
    }

    private class Knight : Piece
    {
        public Knight(PieceColor _color) : base(PieceType.Knight, _color) { }

        public override bool IsLegalMove(Square from, Square to, Board board, PieceType promotion = PieceType.Pawn)
        {
            Piece p = board.Get(to);
            return (p == null || p.color == opponent) && IsCheckedSquare(to, from, board);
        }

        public override bool IsCheckedSquare(Square target, Square current, Board board)
        {
            int ax = Math.Abs(target.file - current.file);
            int ay = Math.Abs(target.rank - current.rank);

            if (ax == 1)
            {
                return ay == 2;
            }
            else if (ax == 2)
            {
                return ay == 1;
            }

            return false;
        }

        public override IEnumerable<Square> LegalMoves(Square from, Board board)
        {
            int[,] squares = { { from.file + 2, from.rank + 1 }, { from.file + 1, from.rank + 2 },
                           { from.file - 1, from.rank + 2 }, { from.file - 2, from.rank + 1 },
                           { from.file - 2, from.rank - 1 }, { from.file - 1, from.rank - 2 },
                           { from.file + 1, from.rank - 2 }, { from.file + 2, from.rank - 1 } };

            for (int i = 0; i < 8; ++i)
            {
                int x = squares[i, 0], y = squares[i, 1];
                if (!Square.Exists(x, y)) continue;

                Square to = Square.At(x, y);
                Piece p = board.Get(to);
                if (p == null || p.color == opponent) yield return to;
            }
        }
    }

    private class Bishop : Piece
    {
        public Bishop(PieceColor _color) : base(PieceType.Bishop, _color) { }

        public override bool IsLegalMove(Square from, Square to, Board board, PieceType promotion = PieceType.Pawn)
        {
            Piece p = board.Get(to);
            return (p == null || p.color == opponent) && IsCheckedSquare(to, from, board);
        }

        public override bool IsCheckedSquare(Square target, Square current, Board board)
        {
            return Math.Abs(target.file - current.file) == Math.Abs(target.rank - current.rank) && board.IsUnblockedPath(current, target);
        }

        public override IEnumerable<Square> LegalMoves(Square from, Board board)
        {
            for (int dir = 1; dir < 8; dir += 2)
            {
                foreach (Square to in from.StraightLine(dir))
                {
                    Piece p = board.Get(to);
                    if (p != null)
                    {
                        if (p.color == opponent) yield return to;
                        break;
                    }

                    yield return to;
                }
            }
        }
    }

    private class Rook : Piece
    {
        public Rook(PieceColor _color) : base(PieceType.Rook, _color) { }

        public override bool IsLegalMove(Square from, Square to, Board board, PieceType promotion = PieceType.Pawn)
        {
            Piece p = board.Get(to);
            return (p == null || p.color == opponent) && IsCheckedSquare(to, from, board);
        }

        public override bool IsCheckedSquare(Square target, Square current, Board board)
        {
            return (current.file == target.file || current.rank == target.rank) && board.IsUnblockedPath(current, target);
        }

        public override IEnumerable<Square> LegalMoves(Square from, Board board)
        {
            for (int dir = 0; dir < 8; dir += 2)
            {
                foreach (Square to in from.StraightLine(dir))
                {
                    Piece p = board.Get(to);
                    if (p != null)
                    {
                        if (p.color == opponent) yield return to;
                        break;
                    }

                    yield return to;
                }
            }
        }

        public override void PreMove(Square from, Square to, Board board, PieceType promotion = PieceType.Pawn)
        {
            board.hasMoved.Add(this);
        }
    }

    private class Queen : Piece
    {
        public Queen(PieceColor _color) : base(PieceType.Queen, _color) { }

        public override bool IsLegalMove(Square from, Square to, Board board, PieceType promotion = PieceType.Pawn)
        {
            Piece p = board.Get(to);
            return (p == null || p.color == opponent) && IsCheckedSquare(to, from, board);
        }

        public override bool IsCheckedSquare(Square target, Square current, Board board)
        {
            int x = target.file - current.file, y = target.rank - current.rank;
            return (x == 0 || y == 0 || Math.Abs(x) == Math.Abs(y)) && board.IsUnblockedPath(current, target);
        }

        public override IEnumerable<Square> LegalMoves(Square from, Board board)
        {
            for (int dir = 0; dir < 8; ++dir)
            {
                foreach (Square to in from.StraightLine(dir))
                {
                    Piece p = board.Get(to);
                    if (p != null)
                    {
                        if (p.color == opponent) yield return to;
                        break;
                    }

                    yield return to;
                }
            }
        }
    }

    private class King : Piece
    {
        public King(PieceColor _color) : base(PieceType.King, _color) { }

        // Assume that TO is a safe square - that is verified in board.IsLegalMove. Do not assume that it is empty.
        public override bool IsLegalMove(Square from, Square to, Board board, PieceType promotion = PieceType.Pawn)
        {
            int x = to.file - from.file, y = to.rank - from.rank;
            if (Math.Abs(x) > 1) // Castling
            {
                if (y != 0 || board.hasMoved.Contains(this)) return false;

                Square rookSquare = (x == 2) ? Square.At(7, from.rank) : (x == -2) ? Square.At(0, from.rank) : null;
                if (rookSquare == null) return false; // This is not the correct distance for a castle

                Piece r = board.Get(rookSquare);
                if (r.type != PieceType.Rook || board.hasMoved.Contains(r) || !board.IsUnblockedPath(from, rookSquare) || board.IsCheckedSquare(from, opponent)) return false;

                Square newRookSquare = (x == 2) ? Square.At(5, from.rank) : Square.At(3, from.rank);
                return !board.IsCheckedSquare(newRookSquare, opponent);
            }

            if (Math.Abs(y) > 1) return false;

            Piece p = board.Get(to);
            return p == null || p.color == opponent;
        }

        public override bool IsCheckedSquare(Square target, Square current, Board board)
        {
            return Math.Abs(target.file - current.file) <= 1 && Math.Abs(target.rank - current.rank) <= 1;
        }

        public override IEnumerable<Square> LegalMoves(Square from, Board board)
        {
            int[,] squares = { { from.file + 1, from.rank }, { from.file + 1, from.rank + 1 },
                           { from.file, from.rank + 1 }, { from.file - 1, from.rank + 1 },
                           { from.file - 1, from.rank }, { from.file - 1, from.rank - 1 },
                           { from.file, from.rank - 1 }, { from.file + 1, from.rank - 1 } };

            for (int i = 0; i < 8; ++i)
            {
                int x = squares[i, 0], y = squares[i, 1];
                if (!Square.Exists(x, y)) continue;

                Square to = Square.At(x, y);
                Piece p = board.Get(to);
                if (p == null || p.color == opponent) yield return to;
            }

            // Castling
            if (board.hasMoved.Contains(this)) yield break;

            Square[] rookSquares = { Square.At(0, from.rank), Square.At(7, from.rank) };
            Square[] newRookSquares = { Square.At(3, from.rank), Square.At(5, from.rank) };
            Square[] newKingSquares = { Square.At(2, from.rank), Square.At(6, from.rank) };
            for (int i = 0; i < 2; ++i)
            {
                Piece p = board.Get(rookSquares[i]);
                if (p.type == PieceType.Rook && !board.hasMoved.Contains(p) && board.IsUnblockedPath(from, rookSquares[i]) && !board.IsCheckedSquare(from, opponent) && !board.IsCheckedSquare(newRookSquares[i], opponent))
                {
                    yield return newKingSquares[i];
                }
            }
        }

        public override void PreMove(Square from, Square to, Board board, PieceType promotion = PieceType.Pawn)
        {
            board.hasMoved.Add(this);

            int x = to.file - from.file;
            if (Math.Abs(x) > 1) // Castling
            {
                Square rookSquare, newRookSquare;
                if (x == 2)
                {
                    rookSquare = Square.At(7, from.rank);
                    newRookSquare = Square.At(5, from.rank);

                }
                else
                {
                    rookSquare = Square.At(0, from.rank);
                    newRookSquare = Square.At(3, from.rank);
                }
                board.Put(newRookSquare, board.Get(rookSquare));
                board.Put(rookSquare, null);
            }
        }
    }
}