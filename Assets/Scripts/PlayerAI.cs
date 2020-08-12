using System;
using System.Collections.Generic;

using BoardStatus = Board.BoardStatus;
using Square = Board.Square;
using PieceType = Board.PieceType;
using PieceColor = Board.PieceColor;
using PieceData = Board.PieceData;
using Move = Board.Move;

#if UNITY_WEBGL || UNITY_EDITOR
using System.Collections;
using System.Diagnostics;
using UnityEngine;

public class PlayerAI : MonoBehaviour
{
    public PieceColor color;

    public bool isCalculating { get { return findMoveCoroutine != null; } }

    public void FindMove(Board board)
    {
        if (findMoveCoroutine != null)
            StopCoroutine(findMoveCoroutine);

        findMoveCoroutine = StartCoroutine(FindMoveRoutine(board));
    }

    Coroutine findMoveCoroutine;
    private IEnumerator FindMoveRoutine(Board board)
    {
        Stopwatch clock = Stopwatch.StartNew();
        IEnumerator<int?> enumerator = FindMove(board, MaxDepth(board), true, color == PieceColor.White, -INFINITY, INFINITY);
        while (enumerator.MoveNext())
        {
            if (clock.Elapsed.TotalSeconds >= 0.001) // TODO: better performance solution: https://stackoverflow.com/questions/2055927/ienumerable-and-recursion-using-yield-return
            {
                yield return null;
                clock.Restart();
            }
        }

        findMoveCoroutine = null;
    }

    private IEnumerator<int?> FindMove(Board board, int depth, bool saveMove, bool max, int alpha, int beta)
    {
        BoardStatus status = board.status;
        if (depth == 0 || (status != BoardStatus.Playing)) // AI shouldn't need to think about the promote state
        {
            yield return StaticScore(board);
            yield break;
        }

        List<Move> moves = new List<Move>(board.legalMoves);
        if (saveMove)
            Shuffle(moves);

        if (max)
        {
            int bestVal = -INFINITY;
            foreach (Move move in moves)
            {
                board.MakeMove(move);
                int moveVal = 0;
                IEnumerator<int?> enumerator = FindMove(board, depth - 1, false, false, alpha, beta);
                yield return null;
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current != null)
                        moveVal = enumerator.Current.Value;
                    yield return null;
                }
                bestVal = Math.Max(bestVal, moveVal);
                board.Undo();

                if (bestVal > beta) // prune; black would never let this happen
                {
                    yield return bestVal;
                    yield break;
                }

                if (bestVal > alpha) // this is the best option for white on the path to root (overall)
                {
                    alpha = bestVal;
                    if (saveMove)
                        foundMove = move;
                }
            }
            yield return bestVal; // the best (maximal) value for white from this board position (not necessarily overall)
        }
        else
        {
            int bestVal = INFINITY;
            foreach (Move move in moves)
            {
                board.MakeMove(move);
                int moveVal = 0;
                IEnumerator<int?> enumerator = FindMove(board, depth - 1, false, true, alpha, beta);
                yield return null;
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current != null)
                        moveVal = enumerator.Current.Value;
                    yield return null;
                }
                bestVal = Math.Min(bestVal, moveVal);
                board.Undo();

                if (bestVal < alpha) // prune; white would never let this happen
                {
                    yield return bestVal;
                    yield break;
                }

                if (bestVal < beta) // this is the best option for black on the path to root (overall)
                {
                    beta = bestVal;
                    if (saveMove)
                        foundMove = move;
                }
            }
            yield return bestVal; // the best (minimal) value for black from this board position (not necessarily overall)
        }
    }
#else
public class PlayerAI
{
    public readonly PieceColor color;

    public PlayerAI(PieceColor _color)
    {
        color = _color;
    }

    /** Return a move for this player from the current position, assuming such a move exists.
     *  Must be this player's turn. BOARD may be modified (copying BOARD is GameManager's responsibility). */
    public void FindMove(object board)
    {
        FindMove((Board)board, MaxDepth((Board)board), true, color == PieceColor.White, -INFINITY, INFINITY);
    }

    /** Find a move from position BOARD and return its value, recording
     *  the move found in lastFoundMove iff SAVEMOVE. The move
     *  should have maximal value or have value > BETA if SENSE,
     *  and minimal value or value < ALPHA otherwise. Searches up to
     *  DEPTH levels.  Searching at level 0 simply returns a static estimate
     *  of the board value and does not set lastMoveFound. */
    private int FindMove(Board board, int depth, bool saveMove, bool max, int alpha, int beta)
    {
        BoardStatus status = board.status;
        if (depth == 0 || (status != BoardStatus.Playing)) // AI shouldn't need to think about the promote state
            return StaticScore(board);

        List<Move> moves = new List<Move>(board.legalMoves);
        if (saveMove)
            Shuffle(moves);

        if (max)
        {
            int bestVal = -INFINITY;
            foreach (Move move in moves)
            {
                board.MakeMove(move);
                bestVal = Math.Max(bestVal, FindMove(board, depth - 1, false, false, alpha, beta));
                board.Undo();

                if (bestVal > beta) // prune; black would never let this happen
                {
                    return bestVal;
                }

                if (bestVal > alpha) // this is the best option for white on the path to root (overall)
                {
                    alpha = bestVal;
                    if (saveMove)
                        foundMove = move;
                }
            }
            return bestVal; // the best (maximal) value for white from this board position (not necessarily overall)
        }
        else
        {
            int bestVal = INFINITY;
            foreach (Move move in moves)
            {
                board.MakeMove(move);
                bestVal = Math.Min(bestVal, FindMove(board, depth - 1, false, true, alpha, beta));
                board.Undo();

                if (bestVal < alpha) // prune; white would never let this happen
                {
                    return bestVal;
                }

                if (bestVal < beta) // this is the best option for black on the path to root (overall)
                {
                    beta = bestVal;
                    if (saveMove)
                        foundMove = move;
                }
            }
            return bestVal; // the best (minimal) value for black from this board position (not necessarily overall)
        }
    }
#endif

    /** The move found by the last call to FindMove. */
    public Move foundMove { get; private set; }

    /** Return a heuristically determined maximum search depth
     *  based on characteristics of BOARD. This can be improved. */
    private int MaxDepth(Board board)
    {
        int depth = 2;
        int N = board.pieceCount;
        if (N <= 20)
            depth += (26 - N) / 6;

        return depth;
    }

    /** Return a heuristic value for BOARD. This can be improved. */
    private int StaticScore(Board board)
    {
        BoardStatus status = board.status;
        if (status == BoardStatus.Checkmate)
        {
            if (board.whoseTurn == PieceColor.White)
                return WIN_VALUE;
            else
                return -WIN_VALUE;
        }
        else if (status != BoardStatus.Playing) // AI shouldn't need to think about the promote state
            return 0;

        // TODO: quantify threats
        int score = 0;
        foreach (Square s in Square.squares)
        {
            PieceData? p = board.GetPiece(s);
            if (p == null)
                continue;

            PieceType type = p.Value.type;
            if (type == PieceType.King)
                continue;

            if (p.Value.color == PieceColor.White)
                score += PIECE_VALUES[(int)type];
            else
                score -= PIECE_VALUES[(int)type];
        }

        return score;
    }

    private static System.Random rng = new System.Random();
    public void Shuffle<T>(IList<T> list) // Fisher-Yates shuffle
    {
        for (int n = list.Count; --n > 0;) // skip element 0 because it would only swap with itself
        {
            int k = rng.Next(n + 1);
            T temp = list[k];
            list[k] = list[n];
            list[n] = temp;
        }
    }

    private const int INFINITY = int.MaxValue; // -INFINITY will not overflow
    private const int WIN_VALUE = INFINITY - 1; // must be strictly less than INFINITY (and -WIN_VALUE > -INFINITY) if we are to guarantee that a move is returned
    private static readonly int[] PIECE_VALUES = { 1, 3, 3, 5, 9 };
}
