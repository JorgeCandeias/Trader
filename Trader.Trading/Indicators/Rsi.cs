namespace Outcompute.Trader.Trading.Indicators;

public class Rsi : IndicatorBase<decimal?, decimal?>
{
    internal const int DefaultPeriods = 14;

    private readonly Identity<decimal?> _root;
    private readonly IIndicatorResult<decimal?> _rsi;

    public Rsi(int periods = DefaultPeriods)
    {
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        Periods = periods;

        _root = Indicator.Identity<decimal?>();
        var up = Indicator.Rma(Indicator.Gain(_root), periods);
        var down = Indicator.Rma(Indicator.AbsLoss(_root), periods);
        _rsi = Indicator.Zip(up, down, (u, d) =>
        {
            if (d == 0) return 100;
            if (u == 0) return 0;
            return 100 - (100 / (1 + u / d));
        });
    }

    public Rsi(IIndicatorResult<decimal?> source, int periods = DefaultPeriods) : this(periods)
    {
        Guard.IsNotNull(source, nameof(source));

        LinkFrom(source);
    }

    public int Periods { get; }

    protected override decimal? Calculate(int index)
    {
        // update the core source and cascade
        _root.Update(index, Source[index]);

        // return the final result
        return _rsi[index];
    }
}

public static partial class Indicator
{
    public static Rsi Rsi(int periods = Indicators.Rsi.DefaultPeriods) => new(periods);

    public static Rsi Rsi(IIndicatorResult<decimal?> source, int periods = Indicators.Rsi.DefaultPeriods) => new(source, periods);
}

public static class RsiEnumerableExtensions
{
    public static IEnumerable<decimal?> Rsi<T>(this IEnumerable<T> source, Func<T, decimal?> selector, int periods = 14)
    {
        Guard.IsNotNull(source, nameof(source));

        using var indicator = Indicator.Rsi(periods);

        foreach (var item in source)
        {
            indicator.Add(selector(item));

            yield return indicator[^1];
        }
    }

    public static IEnumerable<decimal?> Rsi(this IEnumerable<Kline> source, int periods = 14)
    {
        return source.Rsi(x => x.ClosePrice, periods);
    }

    public static IEnumerable<decimal?> Rsi(this IEnumerable<decimal?> source, int periods = 14)
    {
        return source.Rsi(x => x, periods);
    }
}