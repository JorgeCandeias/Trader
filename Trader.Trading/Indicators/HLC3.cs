namespace Outcompute.Trader.Trading.Indicators;

[SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Domain")]
public class HLC3 : Transform<HLC, decimal?>
{
    public HLC3(IndicatorResult<HLC> source)
        : base(source, x => (x.High + x.Low + x.Close) / 3)
    {
    }
}

public static partial class Indicator
{
    public static HLC3 HLC3(this IndicatorResult<HLC> source)
        => new(source);

    public static IEnumerable<decimal?> ToHLC3<T>(this IEnumerable<T> source, Func<T, decimal?> highSelector, Func<T, decimal?> lowSelector, Func<T, decimal?> closeSelector)
        => source.Select(x => new HLC(highSelector(x), lowSelector(x), closeSelector(x))).Identity().HLC3();

    public static IEnumerable<decimal?> HLC3(this IEnumerable<Kline> source)
        => source.ToHLC3(x => x.HighPrice, x => x.LowPrice, x => x.ClosePrice);
}