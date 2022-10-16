namespace checkers;

public class Pair<T, TU>
{
    public Pair(T first, TU second)
    {
        First = first;
        Second = second;
    }

    public T First { get; set; }
    public TU Second { get; set; }

    public void Deconstruct(out T first, out TU second)
    {
        first = First;
        second = Second;
    }
}