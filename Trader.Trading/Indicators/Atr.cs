namespace Outcompute.Trader.Trading.Indicators;

public enum AtrMethod
{
    Sma,
    Rma,
    Ema,
    Wma,
    Hma
}

public class Atr : CompositeIndicator<HLC, decimal?>
{
    internal const int DefaultPeriods = 10;
    internal const AtrMethod DefaultAtrMethod = AtrMethod.Rma;

    private static IndicatorResult<decimal?> Create(IndicatorResult<HLC> source, int periods, AtrMethod method)
    {
        Guard.IsGreaterThanOrEqualTo(periods, 2, nameof(periods));

        return method switch
        {
            AtrMethod.Rma => source.TrueRange(true).Rma(periods),
            AtrMethod.Sma => source.TrueRange(true).Sma(periods),
            AtrMethod.Ema => source.TrueRange(true).Ema(periods),
            AtrMethod.Wma => source.TrueRange(true).Wma(periods),
            AtrMethod.Hma => source.TrueRange(true).Hma(periods),
            _ => throw new ArgumentOutOfRangeException(nameof(method))
        };
    }

    public Atr(IndicatorResult<HLC> source, int periods = DefaultPeriods, AtrMethod method = DefaultAtrMethod)
        : base(source, x => Create(x, periods, method))
    {
    }
}

public static partial class Indicator
{
    public static Atr Atr(this IndicatorResult<HLC> source, int periods = Indicators.Atr.DefaultPeriods, AtrMethod method = Indicators.Atr.DefaultAtrMethod) => new(source, periods, method);

    public static IEnumerable<decimal?> ToAtr<T>(this IEnumerable<T> source, Func<T, decimal?> highSelector, Func<T, decimal?> lowSelector, Func<T, decimal?> closeSelector, int periods = Indicators.Atr.DefaultPeriods, AtrMethod method = Indicators.Atr.DefaultAtrMethod)
        => source.Select(x => new HLC(highSelector(x), lowSelector(x), closeSelector(x))).Identity().Atr(periods, method);

    public static IEnumerable<decimal?> ToAtr(this IEnumerable<Kline> source, int periods = Indicators.Atr.DefaultPeriods, AtrMethod method = Indicators.Atr.DefaultAtrMethod)
        => source.ToAtr(x => x.HighPrice, x => x.LowPrice, x => x.ClosePrice, periods, method);
}