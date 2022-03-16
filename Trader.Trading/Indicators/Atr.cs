namespace Outcompute.Trader.Trading.Indicators;

public enum AtrMethod
{
    Sma,
    Rma,
    Ema,
    Wma,
    Hma
}

public class Atr : IndicatorBase<HLC, decimal?>
{
    internal const int DefaultPeriods = 10;
    internal const AtrMethod DefaultAtrMethod = AtrMethod.Rma;

    private readonly Identity<HLC> _source;
    private readonly IIndicatorResult<decimal?> _indicator;

    public Atr(int periods = DefaultPeriods, AtrMethod method = DefaultAtrMethod)
    {
        Guard.IsGreaterThanOrEqualTo(periods, 2, nameof(periods));

        _source = Indicator.Identity<HLC>();

        _indicator = method switch
        {
            AtrMethod.Rma => Indicator.Rma(Indicator.TrueRange(_source), periods),
            AtrMethod.Sma => Indicator.Sma(Indicator.TrueRange(_source), periods),
            AtrMethod.Ema => Indicator.Ema(Indicator.TrueRange(_source), periods),
            AtrMethod.Wma => Indicator.Wma(Indicator.TrueRange(_source), periods),
            AtrMethod.Hma => Indicator.Hma(Indicator.TrueRange(_source), periods),
            _ => throw new ArgumentOutOfRangeException(nameof(method))
        };

        Periods = periods;
    }

    public Atr(IIndicatorResult<HLC> source, int periods = DefaultPeriods, AtrMethod method = DefaultAtrMethod) : this(periods, method)
    {
        Guard.IsNotNull(source, nameof(source));

        LinkFrom(source);
    }

    public int Periods { get; }

    protected override decimal? Calculate(int index)
    {
        // update the core source and cascade
        _source.Update(index, Source[index]);

        // return the final result
        return _indicator[index];
    }
}

public static class AtrEnumerableExtensions
{
    public static IEnumerable<decimal?> Atr<T>(this IEnumerable<T> source, Func<T, decimal?> highSelector, Func<T, decimal?> lowSelector, Func<T, decimal?> closeSelector, int periods = Indicators.Atr.DefaultPeriods, AtrMethod method = Indicators.Atr.DefaultAtrMethod)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(highSelector, nameof(highSelector));
        Guard.IsNotNull(lowSelector, nameof(lowSelector));
        Guard.IsNotNull(closeSelector, nameof(closeSelector));

        using var indicator = new Atr(periods, method);

        foreach (var item in source)
        {
            indicator.Add(new HLC(highSelector(item), lowSelector(item), closeSelector(item)));

            yield return indicator[^1];
        }
    }

    public static IEnumerable<decimal?> Atr(this IEnumerable<Kline> source, int periods = Indicators.Atr.DefaultPeriods, AtrMethod method = Indicators.Atr.DefaultAtrMethod)
    {
        return source.Atr(x => x.HighPrice, x => x.LowPrice, x => x.ClosePrice, periods, method);
    }
}