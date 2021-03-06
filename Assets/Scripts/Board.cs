﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// TODO
// threefold repetition: a player has the OPTION of claiming a draw if an identical position has occured at least three times during the course of the game with the same player to move each time (the third time CAN be the next position after this player makes their move, i.e. the player can claim the draw before actually making the move)
// fifty move rule: either player has the OPTION of claiming a draw if no capture or pawn movement in the last 50 turns (100 indivial player moves)
// replace takenPieces with Dictionary<int, PieceType> ?

// Representation of an active chess game for use in a game or AI
public class Board
{
    public BoardStatus status { get; private set; } // denotes whether the game is still being played (i.e. still accepting moves), otherwise denotes the gameover condition
    public PieceColor whoseTurn { get; private set; }
    public Square needsPromotion { get; private set; } // the square containing the pawn that needs promotion, or null if none exists
    public Stack<Move> moves { get {
            Move[] arr = new Move[_moves.Count];
            _moves.CopyTo(arr, 0);
            Array.Reverse(arr);
            return new Stack<Move>(arr);
        } }
    public int moveCount { get { return _moves.Count; } }
    public int pieceCount { get; private set; }
    public Move? sideEffect { get {
            Move _sideEffect;
            if (sideEffects.TryGetValue(moveCount - 1, out _sideEffect))
                return _sideEffect;
            else
                return null;
        } }

    private Stack<Move> _moves;

    private Piece[] board;
    private Dictionary<int, Piece> takenPieces; // pieces that have been taken normally (excl. en passant)
    private Dictionary<int, Move> sideEffects; // castles and en passants
    private Dictionary<Piece, int> rookKingFirstMoves; // contains only kings and rooks that have moved at least once
    private Square justDoubleStepped; // for en passant

    public Board()
    {
        board = new Piece[64];
        takenPieces = new Dictionary<int, Piece>();
        sideEffects = new Dictionary<int, Move>();
        rookKingFirstMoves = new Dictionary<Piece, int>();

        _moves = new Stack<Move>();
        pieceCount = 32;

        for (int i = 0; i < 8; ++i)
        {
            Spawn(PieceType.Pawn, PieceColor.White, Square.At(i, 1));
            Spawn(PieceType.Pawn, PieceColor.Black, Square.At(i, 6));
        }
        Spawn(PieceType.Knight, PieceColor.White, Square.At(1, 0));
        Spawn(PieceType.Knight, PieceColor.White, Square.At(6, 0));
        Spawn(PieceType.Knight, PieceColor.Black, Square.At(1, 7));
        Spawn(PieceType.Knight, PieceColor.Black, Square.At(6, 7));
        Spawn(PieceType.Bishop, PieceColor.White, Square.At(2, 0));
        Spawn(PieceType.Bishop, PieceColor.White, Square.At(5, 0));
        Spawn(PieceType.Bishop, PieceColor.Black, Square.At(2, 7));
        Spawn(PieceType.Bishop, PieceColor.Black, Square.At(5, 7));
        Spawn(PieceType.Rook, PieceColor.White, Square.At(0, 0));
        Spawn(PieceType.Rook, PieceColor.White, Square.At(7, 0));
        Spawn(PieceType.Rook, PieceColor.Black, Square.At(0, 7));
        Spawn(PieceType.Rook, PieceColor.Black, Square.At(7, 7));
        Spawn(PieceType.Queen, PieceColor.White, Square.At(3, 0));
        Spawn(PieceType.Queen, PieceColor.Black, Square.At(3, 7));
        Spawn(PieceType.King, PieceColor.White, Square.At(4, 0));
        Spawn(PieceType.King, PieceColor.Black, Square.At(4, 7));
    }

    public Board(Board other)
    {
        status = other.status;
        whoseTurn = other.whoseTurn;
        needsPromotion = other.needsPromotion;

        _moves = other.moves; // copy the stack
        pieceCount = other.pieceCount;

        // because both piece and square objects are stateless (can't be modified), we don't need to deep copy anything
        board = new Piece[64];
        Array.Copy(other.board, board, 64);

        takenPieces = new Dictionary<int, Piece>(other.takenPieces);
        sideEffects = new Dictionary<int, Move>(other.sideEffects);
        rookKingFirstMoves = new Dictionary<Piece, int>(other.rookKingFirstMoves);
        justDoubleStepped = other.justDoubleStepped;
    }

    public PieceData? GetPiece(Square square)
    {
        Piece p = Get(square);
        return (p != null) ? new PieceData(p.type, p.color) : (PieceData?)null;
    }

    private Piece Get(Square square)
    {
        return board[square.rank * 8 + square.file];
    }

    private void Put(Square square, Piece piece)
    {
        board[square.rank * 8 + square.file] = piece;
    }

    private Piece Spawn(PieceType type, PieceColor color, Square square)
    {
        Piece p = Piece.Create(type, color);
        Put(square, p);
        return p;
    }

    // Returns true iff the move is made successfully
    public bool MakeMove(Move move)
    {
        if (!IsLegalMove(move))
            return false;

        // DEBUGboard = new Board(this);
        
        justDoubleStepped = null;

        Piece taken = Get(move.to);
        if (taken != null)
        {
            takenPieces.Add(moveCount, taken);
            --pieceCount;
        }
        Put(move.to, Get(move.from).PreMove(move, this));
        Put(move.from, null);

        _moves.Push(move); // must be called after moveCount

        if (needsPromotion != null)
        {
            status = BoardStatus.Promote;
        }
        else
        {
            whoseTurn = Opponent(whoseTurn);
            if (!legalMoves.Any()) // Game over
            {
                bool kingInCheck = KingInCheck();
                whoseTurn = Opponent(whoseTurn); // turn should be the last player to move
                status = (kingInCheck) ? BoardStatus.Checkmate : BoardStatus.Stalemate;
            }
            else if (InsufficientMaterial())
            {
                whoseTurn = Opponent(whoseTurn);
                status = BoardStatus.InsufficientMaterial;
            }
        }

        return true;
    }
    
    // TODO: handle all of the piece-specific cases in another virtual method
    public bool Undo()
    {
        if (moveCount == 0 || status == BoardStatus.Promote)
            return false;
        
        Move lastMove = _moves.Pop();
        Piece movedPiece = Get(lastMove.to);
        if (lastMove.promotion != PieceType.Pawn)
            movedPiece = Spawn(PieceType.Pawn, movedPiece.color, lastMove.to); // undo promotion

        Put(lastMove.from, movedPiece);
        Piece taken;
        if (takenPieces.TryGetValue(moveCount, out taken)) {
            takenPieces.Remove(moveCount);
            Put(lastMove.to, taken);
            ++pieceCount;
        }
        else
        {
            Put(lastMove.to, null);
        }

        Move _sideEffect;
        if (sideEffects.TryGetValue(moveCount, out _sideEffect))
        {
            sideEffects.Remove(moveCount);
            if (_sideEffect.to == null)
            {
                Spawn(PieceType.Pawn, Opponent(movedPiece.color), _sideEffect.from); // undo en passant
                ++pieceCount;
            }
            else
            {
                // undo castle
                Put(_sideEffect.from, Get(_sideEffect.to));
                Put(_sideEffect.to, null);
            }
        }

        int firstMoveIndex;
        if (rookKingFirstMoves.TryGetValue(movedPiece, out firstMoveIndex) && firstMoveIndex == moveCount) // this piece has now never moved
            rookKingFirstMoves.Remove(movedPiece);

        if (moveCount > 0)
        {
            Move twoMovesAgo = _moves.Peek();
            Piece p = Get(twoMovesAgo.to);
            if (p != null && p.type == PieceType.Pawn && Math.Abs(twoMovesAgo.to.rank - twoMovesAgo.from.rank) == 2)
                justDoubleStepped = twoMovesAgo.to;
            else
                justDoubleStepped = null;
        }
        else
        {
            justDoubleStepped = null;
        }

        if (status == BoardStatus.Playing) // already checked promote
            whoseTurn = Opponent(whoseTurn);
        else
            status = BoardStatus.Playing;

        /*
        if (DEBUGboard != null)
            DEBUGcheckDeformities();
        DEBUGboard = null;
        */

        return true;
    }

    // private Board DEBUGboard;

    /*
    private void DEBUGcheckDeformities()
    {
        if (status != DEBUGboard.status)
        {
            Debug.Log("status deformity");
            Debug.Break();
        }

        if (whoseTurn != DEBUGboard.whoseTurn)
        {
            Debug.Log("whoseTurn deformity");
            Debug.Break();
        }

        if (needsPromotion != DEBUGboard.needsPromotion)
        {
            Debug.Log("needsPromotion deformity");
            Debug.Break();
        }

        Stack<Move> copyOne = moves;
        Stack<Move> copyTwo = DEBUGboard.moves;
        if (copyOne.Count != copyTwo.Count)
        {
            Debug.Log("moves count deformity");
            Debug.Break();
        }
        while (copyOne.Count > 0)
        {
            Move one = copyOne.Pop();
            Move two = copyTwo.Pop();
            if (!one.Equals(two))
            {
                Debug.Log("moves deformity: move # " + copyOne.Count);
                Debug.Log("from = (" + one.from.file + ", " + one.from.rank + "), to = (" + one.to.file + ", " + one.to.rank + ")");
                Debug.Log("from = (" + two.from.file + ", " + two.from.rank + "), to = (" + two.to.file + ", " + two.to.rank + ")");
                Debug.Break();
            }
        }

        int debugcounter = 0;
        foreach (Square s in Square.squares)
        {
            Piece undone = Get(s);
            Piece original = DEBUGboard.Get(s);
            if (undone == null && original == null)
                continue;
            if (undone == null || !undone.Equals(original))
            {
                Debug.Log("board deformity");
                Debug.Log("original:\n" + DEBUGboard.ToString());
                Debug.Log("after undo:\n" + ToString());
                Debug.Break();
                break;
            }

            if (undone != null)
                ++debugcounter;
        }
        if (debugcounter != DEBUGboard.pieceCount)
        {
            Debug.Log("pieceCount deformity");
            Debug.Log("original: " + DEBUGboard.pieceCount);
            Debug.Log("after undo: " + debugcounter);
            Debug.Break();
        }

        if (takenPieces.Count != DEBUGboard.takenPieces.Count)
        {
            Debug.Log("takenPieces count deformity");
            Debug.Break();
        }
        foreach (int i in takenPieces.Keys)
        {
            if (!DEBUGboard.takenPieces.ContainsKey(i) || !takenPieces[i].Equals(DEBUGboard.takenPieces[i]))
            {
                Debug.Log("takenPieces deformity");
                Debug.Break();
            }
        }

        if (sideEffects.Count != DEBUGboard.sideEffects.Count)
        {
            Debug.Log("sideEffects count deformity");
            Debug.Break();
        }
        foreach (int i in sideEffects.Keys)
        {
            if (!DEBUGboard.sideEffects.ContainsKey(i) || !sideEffects[i].Equals(DEBUGboard.sideEffects[i]))
            {
                Debug.Log("sideEffects deformity");
                Debug.Break();
            }
        }

        if (rookKingFirstMoves.Count != DEBUGboard.rookKingFirstMoves.Count)
        {
            Debug.Log("RKFM count deformity");
            Debug.Break();
        }
        foreach (Piece p in rookKingFirstMoves.Keys)
        {
            if (!DEBUGboard.rookKingFirstMoves.ContainsKey(p) || rookKingFirstMoves[p] != DEBUGboard.rookKingFirstMoves[p])
            {
                Debug.Log("rookKingFirstMoves deformity");
                Debug.Break();
            }
        }

        if (justDoubleStepped != DEBUGboard.justDoubleStepped)
        {
            Debug.Log("justDoubleStepped deformity");
            Debug.Log("original: (" + DEBUGboard.justDoubleStepped.file + ", " + DEBUGboard.justDoubleStepped.rank + ")");
            Debug.Log("after undo: (" + justDoubleStepped.file + ", " + justDoubleStepped.rank + ")");
            Debug.Break();
        }
    }
    */

    public bool Promote(PieceType type)
    {
        if (status != BoardStatus.Promote || type == PieceType.Pawn || type == PieceType.King)
            return false;

        Spawn(type, Get(needsPromotion).color, needsPromotion);

        Move lastMove = _moves.Pop();
        _moves.Push(new Move(lastMove.from, lastMove.to, type));

        needsPromotion = null;

        whoseTurn = Opponent(whoseTurn);
        if (!legalMoves.Any())
        {
            bool kingInCheck = KingInCheck();
            whoseTurn = Opponent(whoseTurn);
            status = (kingInCheck) ? BoardStatus.Checkmate : BoardStatus.Stalemate;
        }
        else if (InsufficientMaterial())
        {
            whoseTurn = Opponent(whoseTurn);
            status = BoardStatus.InsufficientMaterial;
        }
        else
        {
            status = BoardStatus.Playing;
        }

        return true;
    }

    public bool IsLegalMove(Move move)
    {
        if (status != BoardStatus.Playing || move.from == null || move.to == null || move.from == move.to)
            return false;

        Piece p = Get(move.from);
        if (p == null || p.color != whoseTurn)
            return false;

        return p.IsLegalMove(move, this) && IsSafeMove(move);
    }

    // True iff whoseTurn's king's square will not be under attack as a result of this move. Assumes FROM -> TO is otherwise a legal move.
    private bool IsSafeMove(Move move)
    {
        // Copy the game state, perform the move, and revert.
        Piece[] boardCopy = new Piece[64];
        Array.Copy(board, boardCopy, 64);
        int pieceCountCopy = pieceCount;
        // we won't be modifying takenPieces
        Dictionary<int, Move> sideEffectsCopy = new Dictionary<int, Move>(sideEffects);
        Dictionary<Piece, int> rookKingFirstMovesCopy = new Dictionary<Piece, int>(rookKingFirstMoves);
        Square justDoubleSteppedCopy = justDoubleStepped;
        Square needsPromotionCopy = needsPromotion; // possibly changed by PreMove

        Put(move.to, Get(move.from).PreMove(move, this));
        Put(move.from, null);
        
        bool kingInCheck = KingInCheck();

        board = boardCopy;
        pieceCount = pieceCountCopy;
        sideEffects = sideEffectsCopy;
        rookKingFirstMoves = rookKingFirstMovesCopy;
        justDoubleStepped = justDoubleSteppedCopy;
        needsPromotion = needsPromotionCopy;

        return !kingInCheck;
    }

    // True iff whoseTurn's king is threatened by an opponent's piece
    public bool KingInCheck()
    {
        Square kingSquare = null;
        Piece p = null;
        foreach (Square s in Square.squares)
        {
            p = Get(s);
            if (p != null && p.type == PieceType.King && p.color == whoseTurn)
            {
                kingSquare = s;
                break;
            }
        }

        if (p == null || p.type != PieceType.King || p.color != whoseTurn)
            throw new Exception("failed to find the current player's king");

        return IsCheckedSquare(kingSquare, Opponent(whoseTurn));
    }

    // True iff COLOR's opponent can move their king to this SQUARE.
    public bool IsCheckedSquare(Square square, PieceColor color)
    {
        foreach (Square s in Square.squares)
        {
            Piece p = Get(s);
            if (p != null && p.color == color && p.IsCheckedSquare(square, s, this))
                return true;
        }

        return false;
    }

    // True iff there is insufficient material for either player to FORCE a checkmate.
    // As of this moment, InsufficientMaterial() does not recognize when a checkmate is already in the process of being forced, so it is not 100% accurate.
    private bool InsufficientMaterial()
    {
        Square whiteBishop = null, blackBishop = null;
        int _pieceCount = 0;
        foreach (Square s in Square.squares)
        {
            Piece p = Get(s);
            if (p == null)
                continue;

            ++_pieceCount;
            if (_pieceCount > 4)
                return false;

            if (p.type == PieceType.King)
                continue;

            if (p.type != PieceType.Knight && p.type != PieceType.Bishop)
                return false;

            if (p.type == PieceType.Bishop)
            {
                if (p.color == PieceColor.White)
                {
                    if (whiteBishop != null)
                        return false;
                    whiteBishop = s;
                }
                else
                {
                    if (blackBishop != null)
                        return false;
                    blackBishop = s;
                }
            }
        }

        return true;

        // TODO: some scenarios (e.g. K + N vs. K + B) can lead to a forced checkmate depending on the starting position
        // If a timer is ever implemented, we could just let the game continue unless checkmate really is impossible
    }

    public IEnumerable<Move> legalMoves
    {
        get
        {
            foreach (Square from in Square.squares)
            {
                Piece p = Get(from);
                if (p == null || p.color != whoseTurn)
                    continue;

                foreach (Move m in p.LegalMoves(from, this))
                {
                    if (IsSafeMove(m))
                        yield return m;
                }
            }
        }
    }

    // True iff all squares are empty between FROM and TO, NONINCLUSIVE. There must be a straight-line path between FROM and TO.
    public bool IsUnblockedPath(Square from, Square to)
    {
        foreach (Square s in from.StraightLine(to))
        {
            if (Get(s) != null)
                return false;
        }

        return true;
    }

    public enum BoardStatus
    {
        Playing, // waiting on a player to make a move
        Promote, // waiting on a player to choose a new piece to replace a pawn that has reached the end of the board
        Checkmate, // self-explanatory
        Stalemate,
        InsufficientMaterial
    }

    //Don't make this a struct (we want singletons with nullability - better suited as a class)
    public class Square
    {
        public readonly int file, rank;

        // Calls StraightLine(dir) in the direction of TO, stopping at TO (without returning it)
        public IEnumerable StraightLine(Square to)
        {
            int x = to.file - file, y = to.rank - rank;
            if (x != 0 && y != 0 && Math.Abs(x) != Math.Abs(y))
                throw new Exception("tried to draw a straight line between invalid squares");

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
                if (s == to)
                    yield break;
                
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
                yield return SQUARES[i];
        }

        private Square(int index)
        {
            rank = index / 8;
            file = index % 8;
        }

        static Square()
        {
            for (int i = 0; i < 64; ++i)
                SQUARES[i] = new Square(i);
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

    public struct PieceData
    {
        public readonly PieceType type;
        public readonly PieceColor color;

        public PieceData(PieceType _type, PieceColor _color)
        {
            type = _type;
            color = _color;
        }
    }

    public struct Move
    {
        public readonly Square from, to;
        public readonly PieceType promotion;

        public Move(Square _from, Square _to, PieceType _promotion = PieceType.Pawn)
        {
            from = _from;
            to = _to;
            promotion = _promotion;
        }
    }

    private abstract class Piece
    {
        public readonly PieceColor color;

        public abstract PieceType type { get; }

        public PieceColor opponent { get { return Opponent(color); } }

        // True iff this piece can make this move on this BOARD. Assume that FROM != TO and that it is this color's turn. Doesn't care if the king is in check or is put into a checked square as a result of this move.
        public abstract bool IsLegalMove(Move move, Board board);
        // True iff a king located at TARGET would be in check due to this piece at its CURRENT position, on the current BOARD. Assume TARGET != CURRENT.
        public abstract bool IsCheckedSquare(Square target, Square current, Board board);

        public abstract IEnumerable<Move> LegalMoves(Square from, Board board);

        // Performs extra steps, depending on the piece's type and the move. Returns the new piece if this piece is replaced (pawn promotion) or this piece otherwise.
        // Assumes the move FROM -> TO is legal, and that promotion != PieceType.King
        // Stop changing this to PostMove! It must be PreMove because justDoubleStepped must be set to null beforehand and PreMove must know whether to en passant
        public virtual Piece PreMove(Move move, Board board) { return this; }

        public static Piece Create(PieceType type, PieceColor color)
        {
            switch(type)
            {
                case PieceType.Pawn:
                    return new Pawn(color);
                case PieceType.Knight:
                    return new Knight(color);
                case PieceType.Bishop:
                    return new Bishop(color);
                case PieceType.Rook:
                    return new Rook(color);
                case PieceType.Queen:
                    return new Queen(color);
                default:
                    return new King(color);
            }
        }

        public bool Equals(Piece other)
        {
            return color == other.color && type == other.type;
        }

        public override bool Equals(object obj)
        {
            return obj != null && GetType() == obj.GetType() && Equals((Piece)obj);
        }

        /* From the spec: "If GetHashCode is not overridden, hash codes for reference types are computed
         * by calling the Object.GetHashCode method of the base class, which computes a hash code based on
         * an object's reference; for more information, see RuntimeHelpers.GetHashCode. In other words,
         * two objects for which the ReferenceEquals method returns true have identical hash codes. */

        // Don't override GetHashCode, because rookKingFirstMoves must distinguish between different rooks of the same color!

        private Piece(PieceColor _color)
        {
            color = _color;
        }

        private class Pawn : Piece
        {
            public override PieceType type { get { return PieceType.Pawn; } }

            public Pawn(PieceColor _color) : base(_color) { }

            public override bool IsLegalMove(Move move, Board board)
            {
                if (move.promotion == PieceType.King)
                    return false;

                int steps = (color == PieceColor.White) ? move.to.rank - move.from.rank : move.from.rank - move.to.rank;
                if (move.from.file == move.to.file)
                    return board.Get(move.to) == null && (steps == 1 || steps == 2 && move.from.rank == ((color == PieceColor.White) ? 1 : 6) && board.IsUnblockedPath(move.from, move.to));

                if (steps == 1 && Math.Abs(move.to.file - move.from.file) == 1)
                {
                    Piece p = board.Get(move.to);
                    if (p != null)
                        return p.color == opponent;

                    // En passant
                    return Square.At(move.to.file, move.from.rank) == board.justDoubleStepped;
                }

                return false;
            }

            public override bool IsCheckedSquare(Square target, Square current, Board board)
            {
                int steps = (color == PieceColor.White) ? target.rank - current.rank : current.rank - target.rank;
                return steps == 1 && Math.Abs(target.file - current.file) == 1;
            }

            public override IEnumerable<Move> LegalMoves(Square from, Board board)
            {
                int y = (color == PieceColor.White) ? from.rank + 1 : from.rank - 1;
                Square to = Square.At(from.file, y);
                if (board.Get(to) == null)
                {
                    foreach (Move m in LegalMovesHelper(from, to))
                        yield return m;

                    if (from.rank == ((color == PieceColor.White) ? 1 : 6)) // en passant
                    {
                        to = Square.At(from.file, (color == PieceColor.White) ? 3 : 4);
                        if (board.Get(to) == null)
                            yield return new Move(from, to);
                    }
                }

                int[] files = { from.file - 1, from.file + 1 };
                foreach (int x in files)
                {
                    if (Square.Exists(x, y))
                    {
                        to = Square.At(x, y);
                        Piece p = board.Get(to);
                        if (p != null)
                        {
                            if (p.color == opponent)
                            {
                                foreach (Move m in LegalMovesHelper(from, to))
                                    yield return m;
                            }
                        }
                        else if (Square.At(x, from.rank) == board.justDoubleStepped) {
                            yield return new Move(from, to);
                        }
                    }
                }
            }

            // takes promotion possibilities into account
            private IEnumerable<Move> LegalMovesHelper(Square from, Square to)
            {
                if (to.rank == 0 || to.rank == 7)
                {
                    yield return new Move(from, to, PieceType.Knight);
                    yield return new Move(from, to, PieceType.Bishop);
                    yield return new Move(from, to, PieceType.Rook);
                    yield return new Move(from, to, PieceType.Queen);
                }
                else
                {
                    yield return new Move(from, to);
                }
            }

            public override Piece PreMove(Move move, Board board)
            {
                if (move.to.rank == 7 || move.to.rank == 0)
                {
                    if (move.promotion == PieceType.Pawn)
                        board.needsPromotion = move.to; // Delay piece selection
                    else
                        return board.Spawn(move.promotion, color, move.from);
                }
                else if (Math.Abs(move.to.rank - move.from.rank) == 2)
                {
                    board.justDoubleStepped = move.to;
                }
                else if (move.from.file != move.to.file && board.Get(move.to) == null) // En passant
                {
                    Square enPassantSquare = Square.At(move.to.file, move.from.rank);
                    board.Put(enPassantSquare, null);
                    board.sideEffects.Add(board.moveCount, new Move(enPassantSquare, null));
                    --board.pieceCount;
                }

                return this;
            }
        }

        private class Knight : Piece
        {
            public override PieceType type { get { return PieceType.Knight; } }

            public Knight(PieceColor _color) : base(_color) { }

            public override bool IsLegalMove(Move move, Board board)
            {
                if (move.promotion != PieceType.Pawn)
                    return false;

                Piece p = board.Get(move.to);
                return (p == null || p.color == opponent) && IsCheckedSquare(move.to, move.from, board);
            }

            public override bool IsCheckedSquare(Square target, Square current, Board board)
            {
                int ax = Math.Abs(target.file - current.file);
                int ay = Math.Abs(target.rank - current.rank);

                if (ax == 1)
                    return ay == 2;
                else if (ax == 2)
                    return ay == 1;

                return false;
            }

            public override IEnumerable<Move> LegalMoves(Square from, Board board)
            {
                int[,] squares = { { from.file + 2, from.rank + 1 }, { from.file + 1, from.rank + 2 },
                               { from.file - 1, from.rank + 2 }, { from.file - 2, from.rank + 1 },
                               { from.file - 2, from.rank - 1 }, { from.file - 1, from.rank - 2 },
                               { from.file + 1, from.rank - 2 }, { from.file + 2, from.rank - 1 } };

                for (int i = 0; i < 8; ++i)
                {
                    int x = squares[i, 0], y = squares[i, 1];
                    if (!Square.Exists(x, y))
                        continue;

                    Square to = Square.At(x, y);
                    Piece p = board.Get(to);
                    if (p == null || p.color == opponent)
                        yield return new Move(from, to);
                }
            }
        }

        private class Bishop : Piece
        {
            public override PieceType type { get { return PieceType.Bishop; } }

            public Bishop(PieceColor _color) : base(_color) { }

            public override bool IsLegalMove(Move move, Board board)
            {
                if (move.promotion != PieceType.Pawn)
                    return false;

                Piece p = board.Get(move.to);
                return (p == null || p.color == opponent) && IsCheckedSquare(move.to, move.from, board);
            }

            public override bool IsCheckedSquare(Square target, Square current, Board board)
            {
                return Math.Abs(target.file - current.file) == Math.Abs(target.rank - current.rank) && board.IsUnblockedPath(current, target);
            }

            public override IEnumerable<Move> LegalMoves(Square from, Board board)
            {
                for (int dir = 1; dir < 8; dir += 2)
                {
                    foreach (Square to in from.StraightLine(dir))
                    {
                        Piece p = board.Get(to);
                        if (p != null)
                        {
                            if (p.color == opponent)
                                yield return new Move(from, to);
                            break;
                        }

                        yield return new Move(from, to);
                    }
                }
            }
        }

        private class Rook : Piece
        {
            public override PieceType type { get { return PieceType.Rook; } }

            public Rook(PieceColor _color) : base(_color) { }

            public override bool IsLegalMove(Move move, Board board)
            {
                if (move.promotion != PieceType.Pawn)
                    return false;

                Piece p = board.Get(move.to);
                return (p == null || p.color == opponent) && IsCheckedSquare(move.to, move.from, board);
            }

            public override bool IsCheckedSquare(Square target, Square current, Board board)
            {
                return (current.file == target.file || current.rank == target.rank) && board.IsUnblockedPath(current, target);
            }

            public override IEnumerable<Move> LegalMoves(Square from, Board board)
            {
                for (int dir = 0; dir < 8; dir += 2)
                {
                    foreach (Square to in from.StraightLine(dir))
                    {
                        Piece p = board.Get(to);
                        if (p != null)
                        {
                            if (p.color == opponent)
                                yield return new Move(from, to);
                            break;
                        }

                        yield return new Move(from, to);
                    }
                }
            }

            public override Piece PreMove(Move move, Board board)
            {
                //board.rookKingFirstMoves.TryAdd(this, board.moveCount); not .NET 2.0 compatible
                if (!board.rookKingFirstMoves.ContainsKey(this))
                    board.rookKingFirstMoves.Add(this, board.moveCount);

                return this;
            }
        }

        private class Queen : Piece
        {
            public override PieceType type { get { return PieceType.Queen; } }

            public Queen(PieceColor _color) : base(_color) { }

            public override bool IsLegalMove(Move move, Board board)
            {
                if (move.promotion != PieceType.Pawn)
                    return false;

                Piece p = board.Get(move.to);
                return (p == null || p.color == opponent) && IsCheckedSquare(move.to, move.from, board);
            }

            public override bool IsCheckedSquare(Square target, Square current, Board board)
            {
                int x = target.file - current.file, y = target.rank - current.rank;
                return (x == 0 || y == 0 || Math.Abs(x) == Math.Abs(y)) && board.IsUnblockedPath(current, target);
            }

            public override IEnumerable<Move> LegalMoves(Square from, Board board)
            {
                for (int dir = 0; dir < 8; ++dir)
                {
                    foreach (Square to in from.StraightLine(dir))
                    {
                        Piece p = board.Get(to);
                        if (p != null)
                        {
                            if (p.color == opponent)
                                yield return new Move(from, to);
                            break;
                        }

                        yield return new Move(from, to);
                    }
                }
            }
        }

        private class King : Piece
        {
            public override PieceType type { get { return PieceType.King; } }

            public King(PieceColor _color) : base(_color) { }

            // Assume that TO is a safe square - that is verified in board.IsLegalMove. Do not assume that it is empty.
            public override bool IsLegalMove(Move move, Board board)
            {
                if (move.promotion != PieceType.Pawn)
                    return false;

                int x = move.to.file - move.from.file, y = move.to.rank - move.from.rank;
                if (Math.Abs(x) > 1) // Castling
                {
                    if (y != 0 || board.rookKingFirstMoves.ContainsKey(this))
                        return false;

                    Square rookSquare = (x == 2) ? Square.At(7, move.from.rank) : (x == -2) ? Square.At(0, move.from.rank) : null;
                    if (rookSquare == null)
                        return false; // This is not the correct distance for a castle

                    Piece r = board.Get(rookSquare);
                    if (r.type != PieceType.Rook || board.rookKingFirstMoves.ContainsKey(r) || !board.IsUnblockedPath(move.from, rookSquare) || board.IsCheckedSquare(move.from, opponent))
                        return false;

                    Square newRookSquare = (x == 2) ? Square.At(5, move.from.rank) : Square.At(3, move.from.rank);
                    return !board.IsCheckedSquare(newRookSquare, opponent);
                }

                if (Math.Abs(y) > 1)
                    return false;

                Piece p = board.Get(move.to);
                return p == null || p.color == opponent;
            }

            public override bool IsCheckedSquare(Square target, Square current, Board board)
            {
                return Math.Abs(target.file - current.file) <= 1 && Math.Abs(target.rank - current.rank) <= 1;
            }

            public override IEnumerable<Move> LegalMoves(Square from, Board board)
            {
                int[,] squares = { { from.file + 1, from.rank }, { from.file + 1, from.rank + 1 },
                               { from.file, from.rank + 1 }, { from.file - 1, from.rank + 1 },
                               { from.file - 1, from.rank }, { from.file - 1, from.rank - 1 },
                               { from.file, from.rank - 1 }, { from.file + 1, from.rank - 1 } };

                for (int i = 0; i < 8; ++i)
                {
                    int x = squares[i, 0], y = squares[i, 1];
                    if (!Square.Exists(x, y))
                        continue;

                    Square to = Square.At(x, y);
                    Piece p = board.Get(to);
                    if (p == null || p.color == opponent)
                        yield return new Move(from, to);
                }

                // Castling
                if (board.rookKingFirstMoves.ContainsKey(this))
                    yield break;

                Square[] rookSquares = { Square.At(0, from.rank), Square.At(7, from.rank) };
                Square[] newRookSquares = { Square.At(3, from.rank), Square.At(5, from.rank) };
                Square[] newKingSquares = { Square.At(2, from.rank), Square.At(6, from.rank) };
                for (int i = 0; i < 2; ++i)
                {
                    Piece p = board.Get(rookSquares[i]);
                    if (p != null && p.type == PieceType.Rook && !board.rookKingFirstMoves.ContainsKey(p) && board.IsUnblockedPath(from, rookSquares[i]) && !board.IsCheckedSquare(from, opponent) && !board.IsCheckedSquare(newRookSquares[i], opponent))
                        yield return new Move(from, newKingSquares[i]);
                }
            }

            public override Piece PreMove(Move move, Board board)
            {
                //board.rookKingFirstMoves.TryAdd(this, board.moveCount); not .NET 2.0 compatible
                if (!board.rookKingFirstMoves.ContainsKey(this))
                    board.rookKingFirstMoves.Add(this, board.moveCount);

                int x = move.to.file - move.from.file;
                if (Math.Abs(x) > 1) // Castling
                {
                    Square rookSquare, newRookSquare;
                    if (x == 2)
                    {
                        rookSquare = Square.At(7, move.from.rank);
                        newRookSquare = Square.At(5, move.from.rank);
                    }
                    else
                    {
                        rookSquare = Square.At(0, move.from.rank);
                        newRookSquare = Square.At(3, move.from.rank);
                    }
                    board.Put(newRookSquare, board.Get(rookSquare));
                    board.Put(rookSquare, null);

                    board.sideEffects.Add(board.moveCount, new Move(rookSquare, newRookSquare));
                }

                return this;
            }
        }
    }

    // Tests
    static Board()
    {
        Board board = new Board();

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
            if (!board.MakeMove(kasparov_vs_topalov[i]))
                throw new Exception();
        }

        board = new Board();

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
            if (!board.MakeMove(m))
                throw new Exception();
        }
    }
}