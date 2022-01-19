namespace System.Collections.Generic;

public record struct RegressionChannelValue
{
    public bool IsReady { get; init; }
    public decimal Intercept { get; init; }
    public decimal Slope { get; init; }
    public decimal Value { get; init; }
    public decimal Stdev { get; init; }
    public decimal Band { get; init; }
    public decimal Upper { get; init; }
    public decimal Lower { get; init; }
}

public static class LinearRegressionChannelExtensions
{
    public static IEnumerable<RegressionChannelValue> LinearRegressionChannel(this IEnumerable<Kline> source, int periods = 100, decimal k = 2)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThan(periods, 0, nameof(periods));

        var stdevs = source.Select(x => x.ClosePrice).StdDev(periods).GetEnumerator();
        var lsmas = source.LeastSquaresMovingAverage(periods).GetEnumerator();

        while (stdevs.MoveNext() && lsmas.MoveNext())
        {
            var stdev = stdevs.Current;
            var lsma = lsmas.Current;

            var band = stdev * k;
            var lower = lsma.Value - band;
            var upper = lsma.Value + band;

            yield return new RegressionChannelValue
            {
                IsReady = lsma.IsReady,
                Intercept = lsma.Intercept,
                Slope = lsma.Slope,
                Value = lsma.Value,
                Stdev = stdev,
                Band = band,
                Upper = upper,
                Lower = lower
            };
        }
    }
}