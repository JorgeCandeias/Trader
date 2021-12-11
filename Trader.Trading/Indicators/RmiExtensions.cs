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

    public static bool TryGetPriceForRmi(this IEnumerable<decimal> source, decimal rmi, out decimal price, int momentumPeriods = 3, int rmaPeriods = 14, decimal precision = 0.01M, int maxIterations = 100)
    {
        var prevPrice = source.Last();
        var prevRmi = source.LastRmi(momentumPeriods, rmaPeriods);
        var direction = Math.Sign(rmi - prevRmi);

        if (direction == 0)
        {
            price = prevPrice;
            return true;
        }

        // define the initial search range
        var high = direction < 0 ? prevPrice : source.Max() * 2;
        var low = direction > 0 ? prevPrice : source.Min() / 2;

        for (var i = 0; i < maxIterations; i++)
        {
            // probe halfway between the range
            var candidatePrice = (low + high) / 2;
            var candidateRmi = source.Append(candidatePrice).LastRmi(momentumPeriods, rmaPeriods);
            var candidateSign = Math.Sign(candidateRmi - rmi);

            // we want to err on the side of the target rmi
            if (candidateSign == direction)
            {
                var candidateRate = candidateRmi / rmi;
                var candidatePrecision = Math.Abs(1 - candidateRate);
                if (candidatePrecision <= precision)
                {
                    price = candidatePrice;
                    return true;
                }
            }

            // adjust ranges
            if (candidateRmi < rmi)
            {
                low = candidatePrice;
            }
            else
            {
                high = candidatePrice;
            }
        }

        price = 0;
        return false;
    }

    public static bool TryGetPriceForRmi<T>(this IEnumerable<T> source, Func<T, decimal> selector, decimal rmi, out decimal price, int momentumPeriods = 3, int rmaPeriods = 14, decimal precision = 0.01M, int maxIterations = 100)
    {
        return source.Select(selector).TryGetPriceForRmi(rmi, out price, momentumPeriods, rmaPeriods, precision, maxIterations);
    }
}