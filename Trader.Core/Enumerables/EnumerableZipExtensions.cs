namespace Outcompute.Trader.Core.Enumerables;

public static class EnumerableZipExtensions
{
    public static IEnumerable<TResult> Zip<TFirst, TSecond, TThird, TResult>(
        this IEnumerable<TFirst> first,
        IEnumerable<TSecond> second,
        IEnumerable<TThird> third,
        Func<TFirst, TSecond, TThird, TResult> resultSelector)
    {
        return first
            .Zip(second)
            .Zip(third)
            .Select(x => resultSelector(x.First.First, x.First.Second, x.Second));
    }

    public static IEnumerable<TResult> Zip<TFirst, TSecond, TThird, TFourth, TResult>(
        this IEnumerable<TFirst> first,
        IEnumerable<TSecond> second,
        IEnumerable<TThird> third,
        IEnumerable<TFourth> fourth,
        Func<TFirst, TSecond, TThird, TFourth, TResult> resultSelector)
    {
        return first
            .Zip(second)
            .Zip(third)
            .Zip(fourth)
            .Select(x => resultSelector(x.First.First.First, x.First.First.Second, x.First.Second, x.Second));
    }
}