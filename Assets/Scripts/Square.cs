using System.Collections;

//Don't make this a struct (we want singletons with nullability - better suited as a class)
public class Square
{
    public readonly int file, rank;

    public static bool Exists(int file, int rank)
    {
        return file >= 0 && rank >= 0 && file < 8 && rank < 8;
    }

    //Rank and file must not be out of bounds
    public static Square At(int file, int rank)
    {
        return SQUARES[rank * 8 + file];
    }

    public static IEnumerable Iterator()
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
}