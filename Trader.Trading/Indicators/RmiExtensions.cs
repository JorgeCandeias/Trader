using Outcompute.Trader.Trading.Indicators;

namespace System.Collections.Generic;

public static class RmiExtensions
{
    /// <summary>
    /// Yields the Relative Momentum Index over the specified source.
    /// </summary>
    /// <param name="source">The source to calculate the RMI over.</param>
    /// <param name="momentumPeriods">The number of periods to use for momentum comparisons.</param>
    /// <param name="rmaPeriods">The number of periods to use for the Running Moving Average over the momentum mins and maxes.</param>
    public static IEnumerable<decimal?> Rmi<T>(this IEnumerable<T> source, Func<T, decimal?> selector, int momentumPeriods = 3, int rmaPeriods = 14)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));
        Guard.IsGreaterThanOrEqualTo(momentumPeriods, 1, nameof(momentumPeriods));
        Guard.IsGreaterThanOrEqualTo(rmaPeriods, 1, nameof(rmaPeriods));

        var momentum = source.ToMomentum(selector, momentumPeriods);
        var ups = momentum.Maximums(0).ToRma(rmaPeriods).GetEnumerator();
        var downs = momentum.Minimums(0).Opposites().ToRma(rmaPeriods).GetEnumerator();

        while (ups.MoveNext() && downs.MoveNext())
        {
            var down = downs.Current;
            var up = ups.Current;

            if (down == 0M)
            {
                yield return 100M;
            }
            else if (up == 0M)
            {
                yield return 0M;
            }
            else
            {
                yield return 100M - (100M / (1 + up / down));
            }
        }
    }

    /// <inheritdoc cref="Rmi(IEnumerable{decimal}, int, int)"/>
    public static IEnumerable<decimal?> Rmi(this IEnumerable<Kline> source, int momentumPeriods = 3, int rmaPeriods = 14)
    {
        Guard.IsNotNull(source, nameof(source));

        return source.Rmi(x => x.ClosePrice, momentumPeriods, rmaPeriods);
    }
}