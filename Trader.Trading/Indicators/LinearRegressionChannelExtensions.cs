using Outcompute.Trader.Core.Pooling;

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
    public decimal PearsonR { get; init; }
}

public static class LinearRegressionChannelExtensions
{
    public static IEnumerable<RegressionChannelValue> LinearRegressionChannel(this IEnumerable<Kline> source, int periods = 100, decimal k = 2)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThan(periods, 0, nameof(periods));

        var klines = source.GetEnumerator();
        var lsmas = source.LeastSquaresMovingAverage(periods).GetEnumerator();

        var window = QueuePool<Kline>.Shared.Get();

        while (klines.MoveNext() && lsmas.MoveNext())
        {
            var kline = klines.Current;
            var lsma = lsmas.Current;

            window.Enqueue(kline);
            while (window.Count > periods)
            {
                window.Dequeue();
            }

            var upDev = 0M;
            var dnDev = 0M;
            var stdDevAcc = 0M;
            var dsxx = 0M;
            var dsyy = 0M;
            var dsxy = 0M;
            var daY = lsma.Intercept + lsma.Slope * periods / 2;
            var val = lsma.Intercept;

            // specialized stdev calculation for the channel
            foreach (var item in window)
            {
                var price = item.HighPrice - val;

                upDev = Math.Max(upDev, price);

                price = val - item.LowPrice;

                dnDev = Math.Max(dnDev, price);

                price = item.ClosePrice;

                var dxt = price - lsma.Value;
                var dyt = val - daY;
                price -= val;

                stdDevAcc += price * price;
                dsxx += dxt * dxt;
                dsyy += dyt * dyt;
                dsxy += dxt * dyt;
                val += lsma.Slope;
            }

            // calculate results
            var stdev = (decimal)Math.Sqrt((double)(stdDevAcc / periods));
            var pearsonBase = dsxx == 0M || dsyy == 0M ? 0M : (decimal)Math.Sqrt((double)(dsxx * dsyy));
            var pearsonR = pearsonBase == 0M ? 0M : dsxy / pearsonBase;

            // calculate the band
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
                PearsonR = pearsonR,
                Band = band,
                Upper = upper,
                Lower = lower
            };
        }

        QueuePool<Kline>.Shared.Return(window);
    }
}