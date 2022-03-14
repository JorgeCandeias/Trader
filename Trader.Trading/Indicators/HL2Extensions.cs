namespace System.Collections.Generic;

public static class HL2Extensions
{
    public static IEnumerable<decimal?> HL2<T>(this IEnumerable<T> source, Func<T, decimal?> highSelector, Func<T, decimal?> lowSelector)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(highSelector, nameof(highSelector));
        Guard.IsNotNull(lowSelector, nameof(lowSelector));

        foreach (var item in source)
        {
            var high = highSelector(item);
            var low = lowSelector(item);

            yield return (high + low) / 2M;
        }
    }

    public static IEnumerable<decimal?> HL2(this IEnumerable<Kline> source)
    {
        return source.HL2(x => x.HighPrice, x => x.LowPrice);
    }
}