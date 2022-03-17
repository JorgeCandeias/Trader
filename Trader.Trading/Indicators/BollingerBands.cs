namespace Outcompute.Trader.Trading.Indicators;

public record class BollingerBand(decimal? Average, decimal? High, decimal? Low)
{
    public static BollingerBand Empty { get; } = new(null, null, null);
}

public class BollingerBands : IndicatorBase<decimal?, BollingerBand>
{
    internal const int DefaultPeriods = 21;
    internal const int DefaultMultipler = 2;

    private readonly Identity<decimal?> _source;
    private readonly IIndicatorResult<BollingerBand> _indicator;

    public BollingerBands(int periods = DefaultPeriods, int multiplier = DefaultMultipler)
    {
        Guard.IsGreaterThanOrEqualTo(periods, 2, nameof(periods));
        Guard.IsGreaterThanOrEqualTo(multiplier, 0, nameof(multiplier));

        Periods = periods;
        Multipler = multiplier;

        _source = Indicator.Identity<decimal?>();

        var average = Indicator.Sma(_source, periods);
        var stdev = Indicator.StDev(_source, periods);
        _indicator = Indicator.Zip(average, stdev, (x, y) => new BollingerBand(x, x + y * multiplier, x - y * multiplier));
    }

    public BollingerBands(IIndicatorResult<decimal?> source, int periods = DefaultPeriods, int multiplier = DefaultMultipler) : this(periods, multiplier)
    {
        Guard.IsNotNull(source, nameof(source));

        LinkFrom(source);
    }

    public int Periods { get; }

    public int Multipler { get; }

    protected override BollingerBand Calculate(int index)
    {
        // update the core source and cascade
        _source.Update(index, Source[index]);

        // return the final result
        return _indicator[index];
    }
}

public partial class Indicator
{
    public static BollingerBands BollingerBands(int periods = Indicators.BollingerBands.DefaultPeriods, int multiplier = Indicators.BollingerBands.DefaultMultipler) => new(periods, multiplier);

    public static BollingerBands BollingerBands(IIndicatorResult<decimal?> source, int periods = Indicators.BollingerBands.DefaultPeriods, int multiplier = Indicators.BollingerBands.DefaultMultipler) => new(source, periods, multiplier);
}

public static class BollingerBandsEnumerableExtensions
{
    public static IEnumerable<BollingerBand> BollingerBands<T>(this IEnumerable<T> source, Func<T, decimal?> selector, int periods = Indicators.BollingerBands.DefaultPeriods, int multiplier = Indicators.BollingerBands.DefaultMultipler)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));

        using var indicator = Indicator.BollingerBands(periods, multiplier);

        foreach (var item in source)
        {
            indicator.Add(selector(item));

            yield return indicator[^1];
        }
    }

    public static IEnumerable<BollingerBand> BollingerBands(this IEnumerable<Kline> source, int periods = Indicators.BollingerBands.DefaultPeriods, int multiplier = Indicators.BollingerBands.DefaultMultipler)
    {
        return source.BollingerBands(x => x.ClosePrice, periods, multiplier);
    }
}