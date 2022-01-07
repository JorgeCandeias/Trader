namespace System.Collections.Generic;

public static class MacdExtensions
{
    public record MacdValue
    {
        public decimal Price { get; init; }
        public decimal Fast { get; init; }
        public decimal Slow { get; init; }
        public decimal Macd { get; init; }
        public decimal Signal { get; init; }
        public decimal Histogram { get; init; }

        public bool IsUptrend { get; init; }
        public bool IsDowntrend { get; init; }
        public bool IsNeutral { get; init; }

        public bool IsUpcross { get; init; }
        public bool IsDowncross { get; init; }

        public static MacdValue Empty { get; } = new MacdValue();
    }

    public static IEnumerable<MacdValue> Macd<T>(this IEnumerable<T> source, Func<T, decimal> selector, int fastLength = 12, int slowLength = 26, int signalStrength = 9)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));

        return source.Select(selector).Macd(fastLength, slowLength, signalStrength);
    }

    public static IEnumerable<MacdValue> Macd(this IEnumerable<decimal> source, int fastLength = 12, int slowLength = 26, int signalStrength = 9)
    {
        Guard.IsNotNull(source, nameof(source));

        var fastEmas = source.Ema(fastLength);
        var slowEmas = source.Ema(slowLength);
        var macds = fastEmas.Zip(slowEmas).Select(zip => zip.First - zip.Second);
        var signals = macds.Ema(signalStrength);

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

                IsUpcross = prev is not null && uptrend && (prev.IsDowntrend || prev.IsNeutral),
                IsDowncross = prev is not null && downtrend && (prev.IsUptrend || prev.IsNeutral)
            };

            yield return value;

            prev = value;
        }
    }

    public static bool TryGetMacdForUpcross<T>(this IEnumerable<T> source, Func<T, decimal> selector, out MacdValue value, int fastLength = 12, int slowLength = 26, int signalStrength = 9, decimal precision = 0.001M, int iterations = 100)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));

        return source.Select(selector).TryGetMacdForUpcross(out value, fastLength, slowLength, signalStrength, precision, iterations);
    }

    public static bool TryGetMacdForUpcross(this IEnumerable<decimal> source, out MacdValue value, int fastLength = 12, int slowLength = 26, int signalStrength = 9, decimal precision = 0.001M, int iterations = 100)
    {
        Guard.IsNotNull(source, nameof(source));

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

    public static bool TryGetMacdForDowncross<T>(this IEnumerable<T> source, Func<T, decimal> selector, out MacdValue value, int fastLength = 12, int slowLength = 26, int signalStrength = 9, decimal precision = 0.001M, int iterations = 100)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));

        return source.Select(selector).TryGetMacdForDowncross(out value, fastLength, slowLength, signalStrength, precision, iterations);
    }

    public static bool TryGetMacdForDowncross(this IEnumerable<decimal> source, out MacdValue value, int fastLength = 12, int slowLength = 26, int signalStrength = 9, decimal precision = 0.001M, int iterations = 100)
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
}