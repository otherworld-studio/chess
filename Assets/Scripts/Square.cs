using System;
using System.Collections;
using UnityEngine;

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
        switch(Math.Sign(x))
        {
            case 1:
                switch(Math.Sign(y))
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
                switch(Math.Sign(y))
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
                switch(Math.Sign(y))
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
        return file >= 0 && rank >= 0 && file < 8 && rank < 8;
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