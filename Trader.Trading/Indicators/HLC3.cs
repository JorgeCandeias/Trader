namespace Outcompute.Trader.Trading.Indicators;

[SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Domain")]
public class HLC3 : Transform<HLC, decimal?>
{
    public HLC3() : base(Transform)
    {
    }

    public HLC3(IIndicatorResult<HLC> source) : base(source, Transform)
    {
    }

    private static readonly Func<HLC, decimal?> Transform = x => (x.High + x.Low + x.Close) / 3;
}

public static partial class Indicator
{
    public static HLC3 HLC3() => new();

    public static HLC3 HLC3(IIndicatorResult<HLC> source) => new(source);
}

[SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Domain")]
public static class HLC3EnumerableExtensions
{
    public static IEnumerable<decimal?> HLC3<T>(this IEnumerable<T> source, Func<T, decimal?> highSelector, Func<T, decimal?> lowSelector, Func<T, decimal?> closeSelector)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(highSelector, nameof(highSelector));
        Guard.IsNotNull(lowSelector, nameof(lowSelector));
        Guard.IsNotNull(closeSelector, nameof(closeSelector));

        using var indicator = new HLC3();

        foreach (var item in source)
        {
            indicator.Add(new HLC(highSelector(item), lowSelector(item), closeSelector(item)));

            yield return indicator[^1];
        }
    }

    public static IEnumerable<decimal?> HLC3(this IEnumerable<Kline> source)
    {
        return source.HLC3(x => x.HighPrice, x => x.LowPrice, x => x.ClosePrice);
    }
}