using Outcompute.Trader.Trading.Indicators;

namespace System.Collections.Generic;

public record struct MacdValue
{
    public decimal? Price { get; init; }
    public decimal? Fast { get; init; }
    public decimal? Slow { get; init; }
    public decimal? Macd { get; init; }
    public decimal? Signal { get; init; }
    public decimal? Histogram { get; init; }

    public bool IsUptrend { get; init; }
    public bool IsDowntrend { get; init; }
    public bool IsNeutral { get; init; }

    public bool IsUpcross { get; init; }
    public bool IsDowncross { get; init; }

    public static MacdValue Empty => new MacdValue();
}

public static class MacdExtensions
{
    public static IEnumerable<MacdValue> Macd(this IEnumerable<decimal?> source, int fastLength = 12, int slowLength = 26, int signalStrength = 9)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThanOrEqualTo(fastLength, 1, nameof(fastLength));
        Guard.IsGreaterThanOrEqualTo(slowLength, 1, nameof(slowLength));
        Guard.IsGreaterThanOrEqualTo(signalStrength, 1, nameof(signalStrength));

        var fastEmas = source.ExponentialMovingAverage(fastLength);
        var slowEmas = source.ExponentialMovingAverage(slowLength);
        var macds = fastEmas.Zip(slowEmas).Select(zip => zip.First - zip.Second);
        var signals = macds.ExponentialMovingAverage(signalStrength);

        using var sourceEnumerator = source.GetEnumerator();
        using var fastEnumerator = fastEmas.GetEnumerator();
        using var slowEnumerator = slowEmas.GetEnumerator();
        using var macdEnumerator = macds.GetEnumerator();
        using var signalEnumerator = signals.GetEnumerator();

        MacdValue? prev = null;

        while (sourceEnumerator.MoveNext() && fastEnumerator.MoveNext() && slowEnumerator.MoveNext() && macdEnumerator.MoveNext() && signalEnumerator.MoveNext())
        {
            // pin the props to avoid redundant access
            var price = sourceEnumerator.Current;
            var fast = fastEnumerator.Current;
            var slow = slowEnumerator.Current;
            var macd = macdEnumerator.Current;
            var signal = signalEnumerator.Current;

            var uptrend = macd > signal;
            var downtrend = macd < signal;
            var neutral = macd == signal;

            var value = new MacdValue
            {
                Price = price,
                Fast = fast,
                Slow = slow,
                Macd = macd,
                Signal = signal,
                Histogram = macd - signal,
                IsUptrend = uptrend,
                IsDowntrend = downtrend,
                IsNeutral = neutral,

                IsUpcross = prev.HasValue && uptrend && (prev.Value.IsDowntrend || prev.Value.IsNeutral),
                IsDowncross = prev.HasValue && downtrend && (prev.Value.IsUptrend || prev.Value.IsNeutral)
            };

            yield return value;

            prev = value;
        }
    }

    public static IEnumerable<MacdValue> Macd(this IEnumerable<Kline> source, int fastLength = 12, int slowLength = 26, int signalStrength = 9)
    {
        return source
            .Select(x => (decimal?)x.ClosePrice)
            .Macd(fastLength, slowLength, signalStrength);
    }

    public static bool TryGetMacdForUpcross(this IEnumerable<decimal?> source, out MacdValue value, int fastLength = 12, int slowLength = 26, int signalStrength = 9, int iterations = 100)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThanOrEqualTo(fastLength, 1, nameof(fastLength));
        Guard.IsGreaterThanOrEqualTo(slowLength, 1, nameof(slowLength));
        Guard.IsGreaterThanOrEqualTo(signalStrength, 1, nameof(signalStrength));

        value = MacdValue.Empty;

        var last = source.Macd(fastLength, slowLength, signalStrength).Last();
        if (!last.IsDowntrend)
        {
            return false;
        }

        // define the initial search range
        var high = source.Max() * 2;
        var low = source.Min() / 2;

        for (var i = 0; i < iterations; i++)
        {
            // probe halfway between the range
            var candidatePrice = (low + high) / 2M;
            var candidateMacd = source.Append(candidatePrice).Macd(fastLength, slowLength, signalStrength).Last();

            // keep the best candidate so far
            if (candidateMacd.IsUptrend)
            {
                value = candidateMacd;
            }

            // adjust ranges to search for a better candidate
            if (candidateMacd.IsUptrend)
            {
                high = candidateMacd.Price;
            }
            else if (candidateMacd.IsDowntrend)
            {
                low = candidateMacd.Price;
            }
            else if (candidateMacd.IsNeutral)
            {
                value = candidateMacd;
                break;
            }
        }

        return value != MacdValue.Empty;
    }

    public static bool TryGetMacdForUpcross(this IEnumerable<Kline> source, out MacdValue value, int fastLength = 12, int slowLength = 26, int signalStrength = 9, int iterations = 100)
    {
        return source
            .Select(x => (decimal?)x.ClosePrice)
            .TryGetMacdForUpcross(out value, fastLength, slowLength, signalStrength, iterations);
    }

    public static bool TryGetMacdForDowncross(this IEnumerable<decimal?> source, out MacdValue value, int fastLength = 12, int slowLength = 26, int signalStrength = 9, int iterations = 100)
    {
        Guard.IsNotNull(source, nameof(source));

        value = MacdValue.Empty;

        var last = source.Macd(fastLength, slowLength, signalStrength).Last();
        if (!last.IsUptrend)
        {
            return false;
        }

        // define the initial search range
        var high = source.Max() * 2;
        var low = source.Min() / 2;

        for (var i = 0; i < iterations; i++)
        {
            // probe halfway between the range
            var candidatePrice = (low + high) / 2M;
            var candidateMacd = source.Append(candidatePrice).Macd(fastLength, slowLength, signalStrength).Last();

            // keep the best candidate so far
            if (candidateMacd.IsDowntrend)
            {
                value = candidateMacd;
            }

            // adjust ranges to search for a better candidate
            if (candidateMacd.IsUptrend)
            {
                high = candidateMacd.Price;
            }
            else if (candidateMacd.IsDowntrend)
            {
                low = candidateMacd.Price;
            }
            else if (candidateMacd.IsNeutral)
            {
                value = candidateMacd;
                break;
            }
        }

        return value != MacdValue.Empty;
    }

    public static bool TryGetMacdForDowncross(this IEnumerable<Kline> source, out MacdValue value, int fastLength = 12, int slowLength = 26, int signalStrength = 9, int iterations = 100)
    {
        return source
            .Select(x => (decimal?)x.ClosePrice)
            .TryGetMacdForDowncross(out value, fastLength, slowLength, signalStrength, iterations);
    }
}