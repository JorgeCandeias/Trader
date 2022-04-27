namespace Outcompute.Trader.Indicators;

public sealed class Identity<TResult> : IndicatorRootBase<TResult>, IIndicatorSource<TResult>
{
    public void Add(TResult value)
    {
        Set(Count, value);
    }

    public void Update(int index, TResult value)
    {
        Set(index, value);
    }
}

public static partial class Indicator
{
    public static Identity<T> Identity<T>() => new();

    public static Identity<T> Identity<T>(params T[] array)
    {
        var identity = new Identity<T>();

        foreach (var item in array)
        {
            identity.Add(item);
        }

        return identity;
    }

    public static Identity<T> Identity<T>(this IEnumerable<T> source)
    {
        var identity = new Identity<T>();

        foreach (var item in source)
        {
            identity.Add(item);
        }

        return identity;
    }
}