namespace Outcompute.Trader.Trading.Indicators;

public class HL2 : Transform<HL, decimal?>
{
    public HL2() : base(Transform)
    {
    }

    public HL2(IIndicatorResult<HL> source) : base(source, Transform)
    {
    }

    private static readonly Func<HL, decimal?> Transform = x => (x.High + x.Low) / 2;
}

public static partial class Indicator
{
    public static HL2 HL2() => new();

    public static HL2 HL2(IIndicatorResult<HL> source) => new(source);
}

public static class HL2EnumerableExtensions
{
    public static IEnumerable<decimal?> HL2<T>(this IEnumerable<T> source, Func<T, decimal?> highSelector, Func<T, decimal?> lowSelector)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(highSelector, nameof(highSelector));
        Guard.IsNotNull(lowSelector, nameof(lowSelector));

        using var indicator = Indicator.HL2();

        foreach (var value in source)
        {
            indicator.Add(new HL(highSelector(value), lowSelector(value)));

            yield return indicator[^1];
        }
    }

    public static IEnumerable<decimal?> HL2(this IEnumerable<Kline> source)
    {
        return source.HL2(x => x.HighPrice, x => x.LowPrice);
    }
}