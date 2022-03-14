namespace Outcompute.Trader.Trading.Indicators;

public static class StochasticFunctionExtensions
{
    public static IEnumerable<decimal?> StochasticFunction(this IEnumerable<(decimal? Value, decimal? High, decimal? Low)> source, int length = 14)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThanOrEqualTo(length, 1, nameof(length));

        var queue = new Queue<(decimal? Source, decimal? High, decimal? Low)>();
        var enumerator = source.GetEnumerator();

        for (var i = 0; i < length - 1; i++)
        {
            if (!enumerator.MoveNext())
            {
                yield break;
            }

            queue.Enqueue(enumerator.Current);

            yield return null;
        }

        while (enumerator.MoveNext())
        {
            var current = enumerator.Current;

            queue.Enqueue(current);

            decimal? lowest = null;
            decimal? highest = null;
            foreach (var (value, high, low) in queue)
            {
                if (lowest.HasValue)
                {
                    if (low.HasValue && low.Value < lowest.Value)
                    {
                        lowest = low;
                    }
                }
                else
                {
                    lowest = low;
                }

                if (highest.HasValue)
                {
                    if (high.HasValue && high.Value > highest.Value)
                    {
                        highest = high;
                    }
                }
                else
                {
                    highest = high;
                }
            }

            if (current.Value.HasValue && lowest.HasValue && highest.HasValue)
            {
                yield return 100M * (current.Value.Value - lowest.Value) / (highest.Value - lowest.Value);
            }
            else
            {
                yield return null;
            }

            queue.Dequeue();
        }
    }

    public static IEnumerable<decimal?> StochasticFunction<T>(this IEnumerable<T> source, Func<T, decimal?> valueSelector, Func<T, decimal?> highSelector, Func<T, decimal?> lowSelector, int length = 14)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(valueSelector, nameof(valueSelector));
        Guard.IsNotNull(highSelector, nameof(highSelector));
        Guard.IsNotNull(lowSelector, nameof(lowSelector));
        Guard.IsGreaterThanOrEqualTo(length, 1, nameof(length));

        return source
            .Select(x => (valueSelector(x), highSelector(x), lowSelector(x)))
            .StochasticFunction(length);
    }

    public static IEnumerable<decimal?> StochasticFunction(this IEnumerable<Kline> source, int length = 14)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThanOrEqualTo(length, 1, nameof(length));

        return source.StochasticFunction(x => x.ClosePrice, x => x.HighPrice, x => x.LowPrice);
    }
}