namespace System.Collections.Generic;

[SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Domain")]
public static class HLC3Extensions
{
    public static IEnumerable<decimal?> HLC3<T>(this IEnumerable<T> source, Func<T, decimal?> highSelector, Func<T, decimal?> lowSelector, Func<T, decimal?> closeSelector)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(highSelector, nameof(highSelector));
        Guard.IsNotNull(lowSelector, nameof(lowSelector));

        foreach (var item in source)
        {
            var high = highSelector(item);
            var low = lowSelector(item);
            var close = closeSelector(item);

            yield return (high + low + close) / 3M;
        }
    }

    public static IEnumerable<decimal?> HLC3(this IEnumerable<Kline> source)
    {
        return source.HLC3(x => x.HighPrice, x => x.LowPrice, x => x.ClosePrice);
    }
}