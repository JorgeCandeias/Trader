namespace Outcompute.Trader.Indicators;

public record class BollingerBand(decimal? Average, decimal? High, decimal? Low)
{
    public static BollingerBand Empty { get; } = new(null, null, null);
}

public class BollingerBands : CompositeIndicator<decimal?, BollingerBand>
{
    public const int DefaultPeriods = 21;
    public const int DefaultMultipler = 2;

    public BollingerBands(IndicatorResult<decimal?> source, int periods = DefaultPeriods, int multiplier = DefaultMultipler)
        : base(source, x =>
        {
            Guard.IsGreaterThanOrEqualTo(periods, 2, nameof(periods));
            Guard.IsGreaterThanOrEqualTo(multiplier, 0, nameof(multiplier));

            var average = Indicator.Sma(source, periods);
            var stdev = Indicator.StDev(source, periods);

            return Indicator.Zip(average, stdev, (x, y) => new BollingerBand(x, x + y * multiplier, x - y * multiplier));
        })
    {
    }
}

public partial class Indicator
{
    public static BollingerBands BollingerBands(this IndicatorResult<decimal?> source, int periods = Indicators.BollingerBands.DefaultPeriods, int multiplier = Indicators.BollingerBands.DefaultMultipler)
        => new(source, periods, multiplier);

    public static IEnumerable<BollingerBand> ToBollingerBands<T>(this IEnumerable<T> source, Func<T, decimal?> selector, int periods = Indicators.BollingerBands.DefaultPeriods, int multiplier = Indicators.BollingerBands.DefaultMultipler)
        => source.Select(selector).Identity().BollingerBands(periods, multiplier);
}