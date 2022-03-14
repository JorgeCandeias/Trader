namespace Outcompute.Trader.Trading.Indicators;

public static class RelativeStrengthIndexExtensions
{
    /// <summary>
    /// Calculates the RSI over the specified source.
    /// </summary>
    /// <param name="source">The source for RSI calculation.</param>
    /// <param name="periods">The number of periods for RSI calculation.</param>
    public static IEnumerable<decimal?> RelativeStrengthIndex(this IEnumerable<decimal?> source, int periods)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        using var avgGain = source.Gain().RunningMovingAverage(periods).GetEnumerator();
        using var avgLoss = source.AbsLoss().RunningMovingAverage(periods).GetEnumerator();

        while (avgGain.MoveNext() && avgLoss.MoveNext())
        {
            var loss = avgLoss.Current;
            var gain = avgGain.Current;

            if (loss.HasValue && gain.HasValue)
            {
                if (loss.Value == 0)
                {
                    if (gain.Value == 0)
                    {
                        yield return 50M;
                    }
                    else
                    {
                        yield return 100m;
                    }
                }
                else
                {
                    var rs = gain.Value / loss.Value;

                    yield return 100m - 100m / (1m + rs);
                }
            }
            else
            {
                yield return null;
            }
        }
    }

    /// <inheritdoc cref="Rsi(IEnumerable{decimal}, int)"/>
    /// <param name="selector">A transform function to apply to each element.</param>
    public static IEnumerable<decimal?> RelativeStrengthIndex(this IEnumerable<Kline> source, int periods)
    {
        return source.Close().RelativeStrengthIndex(periods);
    }
}