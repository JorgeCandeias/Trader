namespace Outcompute.Trader.Trading.Indicators;

public class HL2 : Transform<HL, decimal?>
{
    public HL2(IndicatorResult<HL> source)
        : base(source, x => (x.High + x.Low) / 2)
    {
    }
}

public static partial class Indicator
{
    public static HL2 HL2(this IndicatorResult<HL> source)
        => new(source);

    public static IEnumerable<decimal?> ToHL2<T>(this IEnumerable<T> source, Func<T, decimal?> highSelector, Func<T, decimal?> lowSelector)
        => source.Select(x => new HL(highSelector(x), lowSelector(x))).Identity().HL2();

    public static IEnumerable<decimal?> ToHL2(this IEnumerable<Kline> source)
        => source.ToHL2(x => x.HighPrice, x => x.LowPrice);
}