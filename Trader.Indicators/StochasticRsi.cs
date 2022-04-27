namespace Outcompute.Trader.Indicators;

public record struct StochasticRsiResult(decimal? K, decimal? D)
{
    public static StochasticRsiResult Empty { get; } = new();
}

public class StochasticRsi : CompositeIndicator<decimal?, StochasticRsiResult>
{
    public const int DefaultSmoothK = 3;
    public const int DefaultSmoothD = 3;
    public const int DefaultPeriodsRsi = 14;
    public const int DefaultPeriodsStoch = 14;

    public StochasticRsi(IndicatorResult<decimal?> source, int smoothK = DefaultSmoothK, int smoothD = DefaultSmoothD, int periodsRsi = DefaultPeriodsRsi, int periodsStoch = DefaultPeriodsStoch)
        : base(source, x =>
        {
            Guard.IsNotNull(source, nameof(source));
            Guard.IsGreaterThanOrEqualTo(smoothK, 1, nameof(smoothK));
            Guard.IsGreaterThanOrEqualTo(smoothD, 1, nameof(smoothD));
            Guard.IsGreaterThanOrEqualTo(periodsRsi, 1, nameof(periodsRsi));
            Guard.IsGreaterThanOrEqualTo(periodsStoch, 1, nameof(periodsStoch));

            var rsi = Indicator.Rsi(source, periodsRsi);
            var hlc = Indicator.Transform(rsi, x => new HLC(x, x, x));
            var k = Indicator.Sma(Indicator.Stochastic(hlc, periodsStoch), smoothK);
            var d = Indicator.Sma(k, smoothD);

            return Indicator.Zip(k, d, (x, y) => new StochasticRsiResult(x, y));
        })
    {
    }
}

public static partial class Indicator
{
    public static StochasticRsi StochasticRsi(this IndicatorResult<decimal?> source, int smoothK = Indicators.StochasticRsi.DefaultSmoothK, int smoothD = Indicators.StochasticRsi.DefaultSmoothD, int periodsRsi = Indicators.StochasticRsi.DefaultPeriodsRsi, int periodsStoch = Indicators.StochasticRsi.DefaultPeriodsStoch)
        => new(source, smoothK, smoothD, periodsRsi, periodsStoch);
}