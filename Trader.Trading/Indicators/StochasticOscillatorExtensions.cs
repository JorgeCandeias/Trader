namespace Outcompute.Trader.Trading.Indicators;

public record struct StochasticOscillatorResult
{
    public decimal? K { get; init; }
    public decimal? D { get; init; }
}

/*
public class StochasticOscillator : IndicatorBase<HLC, decimal?>
{
    internal const int DefaultPeriodsK = 14;
    internal const int DefaultSmoothK = 1;
    internal const int DefaultPeriodsD = 3;

    private readonly Identity<HLC> _source;
    private readonly IIndicatorResult<decimal?> _stoch;

    public StochasticOscillator(int periodsK = DefaultPeriodsK, int smoothK = DefaultSmoothK, int periodsD = DefaultPeriodsD)
    {
        Guard.IsGreaterThanOrEqualTo(periodsK, 1, nameof(periodsK));
        Guard.IsGreaterThanOrEqualTo(smoothK, 1, nameof(smoothK));
        Guard.IsGreaterThanOrEqualTo(periodsD, 1, nameof(periodsD));

        PeriodsK = periodsK;
        SmoothK = smoothK;
        PeriodsD = periodsD;

        _source = new Identity<HLC>();

        _result = 100M * (close - lowest) / (highest - lowest);
    }

    public StochasticOscillator(IIndicatorResult<HLC> source, int periodsK = DefaultPeriodsK, int smoothK = DefaultSmoothK, int periodsD = DefaultPeriodsD) : this(periods)
    {
        Guard.IsNotNull(source, nameof(source));

        LinkFrom(source);
    }

    public int PeriodsK { get; }
    public int SmoothK { get; }
    public int PeriodsD { get; }

    protected override decimal? Calculate(int index)
    {
        // update the core source and cascade
        _source.Update(index, Source[index]);

        // return the final result
        return _stoch[index];
    }
}

public static partial class Indicator
{
    public static StochasticOscillator StochasticOscillator(int periodsK = Indicators.StochasticOscillator.DefaultPeriodsK, int smoothK = Indicators.StochasticOscillator.DefaultSmoothK, int periodsD = Indicators.StochasticOscillator.DefaultPeriodsD) => new(periodsK, smoothK, periodsD);

    public static StochasticOscillator StochasticOscillator(IIndicatorResult<HLC> source, int periodsK = Indicators.StochasticOscillator.DefaultPeriodsK, int smoothK = Indicators.StochasticOscillator.DefaultSmoothK, int periodsD = Indicators.StochasticOscillator.DefaultPeriodsD) => new(source, periodsK, smoothK, periodsD);
}

*/

public static class StochasticOscillatorExtensions
{
    public static IEnumerable<StochasticOscillatorResult> StochasticOscillator(this IEnumerable<(decimal? Value, decimal? High, decimal? Low)> source, int periodsK = 14, int smoothK = 1, int periodsD = 3)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThanOrEqualTo(periodsK, 1, nameof(periodsK));
        Guard.IsGreaterThanOrEqualTo(smoothK, 1, nameof(smoothK));
        Guard.IsGreaterThanOrEqualTo(periodsD, 1, nameof(periodsD));

        var kf = source.ToStochastic(x => x.High, x => x.Low, x => x.Value, periodsK).ToSma(smoothK);
        var df = kf.ToSma(periodsD);

        var ke = kf.GetEnumerator();
        var de = df.GetEnumerator();

        while (ke.MoveNext() && de.MoveNext())
        {
            yield return new StochasticOscillatorResult
            {
                K = ke.Current,
                D = de.Current
            };
        }
    }

    public static IEnumerable<StochasticOscillatorResult> StochasticOscillator(this IEnumerable<Kline> source, int periodsK = 14, int smoothK = 1, int periodsD = 3)
    {
        return source
            .Select(x => ((decimal?)x.ClosePrice, (decimal?)x.HighPrice, (decimal?)x.LowPrice))
            .StochasticOscillator(periodsK, smoothK, periodsD);
    }
}