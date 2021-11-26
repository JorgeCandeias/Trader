using Outcompute.Trader.Trading.Indicators;

namespace System.Collections.Generic;

public static class RsiExtensions
{
    /// <summary>
    /// Calculates the RSI over the specified source.
    /// </summary>
    /// <param name="source">The source for RSI calculation.</param>
    /// <param name="periods">The number of periods for RSI calculation.</param>
    public static IEnumerable<decimal> Rsi(this IEnumerable<decimal> source, int periods)
    {
        return new RsiIterator(source, periods);
    }

    /// <inheritdoc cref="Rsi(IEnumerable{decimal}, int)"/>
    /// <param name="selector">A transform function to apply to each element.</param>
    public static IEnumerable<decimal> Rsi<T>(this IEnumerable<T> source, Func<T, decimal> selector, int periods)
    {
        var transformed = source.Select(selector);

        return transformed.Rsi(periods);
    }

    public static decimal LastRsi(this IEnumerable<decimal> source, int periods)
    {
        return source.Rsi(periods).Last();
    }

    public static decimal LastRsi<T>(this IEnumerable<T> source, Func<T, decimal> selector, int periods)
    {
        return source.Rsi(selector, periods).Last();
    }

    public static bool TryGetPriceForRsi(this IEnumerable<decimal> source, int periods, decimal rsi, out decimal price, decimal precision = 0.01M, int maxIterations = 100)
    {
        source = source.SkipLast(1);

        var prevPrice = source.Last();
        var prevRsi = source.LastRsi(periods);
        var direction = Math.Sign(rsi - prevRsi);

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
            var candidateRsi = source.Append(candidatePrice).LastRsi(periods);
            var candidateSign = Math.Sign(candidateRsi - rsi);

            // we want to err on the side of the target rsi
            if (candidateSign == direction)
            {
                var candidateRate = candidateRsi / rsi;
                var candidatePrecision = Math.Abs(1 - candidateRate);
                if (candidatePrecision <= precision)
                {
                    price = candidatePrice;
                    return true;
                }
            }

            // adjust ranges
            if (candidateRsi < rsi)
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

    public static bool TryGetPriceForRsi<T>(this IEnumerable<T> source, Func<T, decimal> selector, int periods, decimal rsi, out decimal price, decimal precision = 0.01M, int maxIterations = 100)
    {
        return source.Select(selector).TryGetPriceForRsi(periods, rsi, out price, precision, maxIterations);
    }
}