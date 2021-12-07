namespace System.Collections.Generic;

public static class RmiExtensions
{
    /// <summary>
    /// Yields the Relative Momentum Index over the specified source.
    /// </summary>
    /// <param name="source">The source to calculate the RMI over.</param>
    /// <param name="momentumPeriods">The number of periods to use for momentum comparisons.</param>
    /// <param name="rmaPeriods">The number of periods to use for the Running Moving Average over the momentum mins and maxes.</param>
    public static IEnumerable<decimal> Rmi(this IEnumerable<decimal> source, int momentumPeriods = 3, int rmaPeriods = 14)
    {
        Guard.IsNotNull(source, nameof(source));

        var momentum = source.Momentum(momentumPeriods);
        var ups = momentum.Maximums(0).Rma(rmaPeriods).GetEnumerator();
        var downs = momentum.Minimums(0).Opposites().Rma(rmaPeriods).GetEnumerator();

        while (ups.MoveNext() && downs.MoveNext())
        {
            if (downs.Current == 0M)
            {
                yield return 100M;
            }
            else if (ups.Current == 0M)
            {
                yield return 0M;
            }
            else
            {
                yield return 100M - (100M / (1 + ups.Current / downs.Current));
            }
        }
    }

    /// <inheritdoc cref="Rmi(IEnumerable{decimal}, int, int)"/>
    public static IEnumerable<decimal> Rmi<T>(this IEnumerable<T> source, Func<T, decimal> selector, int momentumPeriods = 3, int rmaPeriods = 14)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));

        return source.Select(selector).Rmi(momentumPeriods, rmaPeriods);
    }

    /// <summary>
    /// Gets the last Relative Momentum Index value of the specified source.
    /// </summary>
    /// <inheritdoc cref="Rmi(IEnumerable{decimal}, int, int)"/>
    public static decimal LastRmi(this IEnumerable<decimal> source, int momentumPeriods = 3, int rmaPeriods = 14)
    {
        Guard.IsNotNull(source, nameof(source));

        return source.Rmi(momentumPeriods, rmaPeriods).Last();
    }

    /// <inheritdoc cref="LastRmi(IEnumerable{decimal}, int, int)"/>
    public static decimal LastRmi<T>(this IEnumerable<T> source, Func<T, decimal> selector, int momentumPeriods = 3, int rmaPeriods = 14)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));

        return source.Select(selector).LastRmi(momentumPeriods, rmaPeriods);
    }
}