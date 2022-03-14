namespace Outcompute.Trader.Trading.Indicators;

public static class StochasticOscillatorExtensions
{
    public record struct StochasticOscillatorValue
    {
        public decimal? K { get; init; }
        public decimal? D { get; init; }
    }

    public static IEnumerable<StochasticOscillatorValue> StochasticOscillator(this IEnumerable<(decimal? Value, decimal? High, decimal? Low)> source, int periodsK = 14, int smoothK = 1, int periodsD = 3)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThanOrEqualTo(periodsK, 1, nameof(periodsK));
        Guard.IsGreaterThanOrEqualTo(smoothK, 1, nameof(smoothK));
        Guard.IsGreaterThanOrEqualTo(periodsD, 1, nameof(periodsD));

        var kf = source.StochasticFunction(periodsK).SimpleMovingAverage(smoothK);
        var df = kf.SimpleMovingAverage(periodsD);

        var ke = kf.GetEnumerator();
        var de = df.GetEnumerator();

        while (ke.MoveNext() && de.MoveNext())
        {
            yield return new StochasticOscillatorValue
            {
                K = ke.Current,
                D = de.Current
            };
        }
    }

    public static IEnumerable<StochasticOscillatorValue> StochasticOscillator(this IEnumerable<Kline> source, int periodsK = 14, int smoothK = 1, int periodsD = 3)
    {
        return source
            .Select(x => ((decimal?)x.ClosePrice, (decimal?)x.HighPrice, (decimal?)x.LowPrice))
            .StochasticOscillator(periodsK, smoothK, periodsD);
    }
}