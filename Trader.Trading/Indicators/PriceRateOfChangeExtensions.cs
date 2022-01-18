namespace System.Collections.Generic;

public static class PriceRateOfChangeExtensions
{
    public static IEnumerable<decimal> PriceRateOfChange(this IEnumerable<Kline> source, int length = 9)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThan(length, 0, nameof(length));

        var queue = new Queue<Kline>(length);
        var enumerator = source.GetEnumerator();

        // enumerate the first items to seed the indicator
        while (queue.Count < length && enumerator.MoveNext())
        {
            queue.Enqueue(enumerator.Current);
            yield return 0;
        }

        // enumerate the remaining items to produce values
        while (enumerator.MoveNext())
        {
            var current = enumerator.Current;
            var previous = queue.Dequeue();

            yield return (current.ClosePrice - previous.ClosePrice) / previous.ClosePrice * 100M;

            queue.Enqueue(current);
        }
    }
}