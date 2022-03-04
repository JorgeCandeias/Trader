namespace System.Collections.Generic;

public record TrixValue
{
    public decimal Price { get; init; }
    public decimal Value { get; init; }
    public decimal Log { get; init; }
    public decimal LogEma3 { get; init; }
    public decimal RoC { get; init; }
    public decimal RoC2 { get; init; }
    public bool IsRoCUp { get; init; }
    public bool IsRoCDown { get; init; }
    public bool IsRoC2Up { get; init; }
    public bool IsRoC2Down { get; init; }

    public static TrixValue Empty { get; } = new TrixValue();
}

public static class TrixExtensions
{
    public static IEnumerable<TrixValue> Trix(this IEnumerable<decimal> source, int periods = 9)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThan(periods, 0, nameof(periods));

        return source.Trix(x => x, periods);
    }

    public static IEnumerable<TrixValue> Trix(this IEnumerable<Kline> source, int periods = 9)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThan(periods, 0, nameof(periods));

        return source.Trix(x => x.ClosePrice, periods);
    }

    public static IEnumerable<TrixValue> Trix<T>(this IEnumerable<T> source, Func<T, decimal> selector, int periods = 9)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThan(periods, 0, nameof(periods));
        Guard.IsNotNull(selector, nameof(selector));

        var sourceEnumerator = source.Select(selector).GetEnumerator();
        var valueEnumerator = source.Select(selector).Ema(periods).Ema(periods).Ema(periods).GetEnumerator();
        var logEnumerator = source.Select(selector).Log().GetEnumerator();
        var emaEnumerator = source.Select(selector).Log().Ema(periods).Ema(periods).Ema(periods).GetEnumerator();
        var rocEnumerator = source.Select(selector).Log().Ema(periods).Ema(periods).Ema(periods).Change().Select(x => x * 10000).GetEnumerator();
        var roc2Enumerator = source.Select(selector).Log().Ema(periods).Ema(periods).Ema(periods).Change().Select(x => x * 10000).Change().GetEnumerator();

        TrixValue? prev = null;

        while (sourceEnumerator.MoveNext() && valueEnumerator.MoveNext() && logEnumerator.MoveNext() && emaEnumerator.MoveNext() && rocEnumerator.MoveNext() && roc2Enumerator.MoveNext())
        {
            var value = new TrixValue
            {
                Price = sourceEnumerator.Current,
                Value = valueEnumerator.Current,
                Log = logEnumerator.Current,
                LogEma3 = emaEnumerator.Current,
                RoC = rocEnumerator.Current,
                RoC2 = roc2Enumerator.Current,
                IsRoCUp = rocEnumerator.Current > prev?.RoC,
                IsRoCDown = rocEnumerator.Current < prev?.RoC,
                IsRoC2Up = roc2Enumerator.Current > prev?.RoC2,
                IsRoC2Down = roc2Enumerator.Current < prev?.RoC2
            };

            yield return value;

            prev = value;
        }
    }

    public static bool TryGetTrixUpSwing(this IEnumerable<Kline> source, out TrixValue value, int periods = 9, int iterations = 100)
    {
        Guard.IsNotNull(source, nameof(source));

        return source.Select(x => x.ClosePrice).TryGetTrixUpSwing(out value, periods, iterations);
    }

    public static bool TryGetTrixUpSwing<T>(this IEnumerable<T> source, Func<T, decimal> selector, out TrixValue value, int periods = 9, int iterations = 100)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));

        return source.Select(selector).TryGetTrixUpSwing(out value, periods, iterations);
    }

    public static bool TryGetTrixUpSwing(this IEnumerable<decimal> source, out TrixValue value, int periods = 9, int iterations = 100)
    {
        Guard.IsNotNull(source, nameof(source));

        value = TrixValue.Empty;

        // the last value must be in downtrend
        var last = source.Trix(periods).Last();
        if (!last.IsRoCDown)
        {
            return false;
        }

        // define the initial search range
        var high = source.Max() * 2M;
        var low = source.Min() / 2M;

        for (var i = 0; i < iterations; i++)
        {
            var candidatePrice = (low + high) / 2;

            // probe halfway between the range
            var candidateTrix = source.Append(candidatePrice).Trix(periods).Last();

            // keep the best candidate so far
            if (candidateTrix.IsRoCUp)
            {
                value = candidateTrix;
            }

            // adjust ranges to search for a better candidate
            if (candidateTrix.IsRoCUp)
            {
                high = candidatePrice;
            }
            else if (candidateTrix.IsRoCDown)
            {
                low = candidatePrice;
            }
            else
            {
                value = candidateTrix;
                return true;
            }
        }

        return value != TrixValue.Empty;
    }

    public static bool TryGetTrixDownSwing(this IEnumerable<Kline> source, out TrixValue value, int periods = 9, int iterations = 100)
    {
        Guard.IsNotNull(source, nameof(source));

        return source.Select(x => x.ClosePrice).TryGetTrixDownSwing(out value, periods, iterations);
    }

    public static bool TryGetTrixDownSwing<T>(this IEnumerable<T> source, Func<T, decimal> selector, out TrixValue value, int periods = 9, int iterations = 100)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));

        return source.Select(selector).TryGetTrixDownSwing(out value, periods, iterations);
    }

    public static bool TryGetTrixDownSwing(this IEnumerable<decimal> source, out TrixValue value, int periods = 9, int iterations = 100)
    {
        Guard.IsNotNull(source, nameof(source));

        value = TrixValue.Empty;

        // the last value must be in downtrend
        var last = source.Trix(periods).Last();
        if (!last.IsRoCUp)
        {
            return false;
        }

        // define the initial search range
        var high = source.Max() * 2M;
        var low = source.Min() / 2M;

        for (var i = 0; i < iterations; i++)
        {
            var candidatePrice = (low + high) / 2;

            // probe halfway between the range
            var candidateTrix = source.Append(candidatePrice).Trix(periods).Last();

            // keep the best candidate so far
            if (candidateTrix.IsRoCDown)
            {
                value = candidateTrix;
            }

            // adjust ranges to search for a better candidate
            if (candidateTrix.IsRoCUp)
            {
                high = candidatePrice;
            }
            else if (candidateTrix.IsRoCDown)
            {
                low = candidatePrice;
            }
            else
            {
                value = candidateTrix;
                return true;
            }
        }

        return value != TrixValue.Empty;
    }

    public static bool TryGetTrixUpAcceleration(this IEnumerable<Kline> source, out TrixValue value, int periods = 9, int iterations = 100)
    {
        Guard.IsNotNull(source, nameof(source));

        return source.Select(x => x.ClosePrice).TryGetTrixUpAcceleration(out value, periods, iterations);
    }

    public static bool TryGetTrixUpAcceleration<T>(this IEnumerable<T> source, Func<T, decimal> selector, out TrixValue value, int periods = 9, int iterations = 100)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));

        return source.Select(selector).TryGetTrixUpAcceleration(out value, periods, iterations);
    }

    public static bool TryGetTrixUpAcceleration(this IEnumerable<decimal> source, out TrixValue value, int periods = 9, int iterations = 100)
    {
        Guard.IsNotNull(source, nameof(source));

        value = TrixValue.Empty;

        // the last value must not be in uptrend
        var last = source.Trix(periods).Last();
        if (last.IsRoC2Up)
        {
            return false;
        }

        // define the initial search range
        var high = source.Max() * 2M;
        var low = source.Min() / 2M;

        for (var i = 0; i < iterations; i++)
        {
            var candidatePrice = (low + high) / 2;

            // probe halfway between the range
            var candidateTrix = source.Append(candidatePrice).Trix(periods).Last();

            // keep the best candidate so far
            if (candidateTrix.IsRoC2Up)
            {
                value = candidateTrix;
            }

            // adjust ranges to search for a better candidate
            if (candidateTrix.IsRoC2Up)
            {
                high = candidatePrice;
            }
            else if (candidateTrix.IsRoC2Down)
            {
                low = candidatePrice;
            }
            else
            {
                value = candidateTrix;
                return true;
            }
        }

        return value != TrixValue.Empty;
    }

    public static bool TryGetTrixDownAcceleration(this IEnumerable<Kline> source, out TrixValue value, int periods = 9, int iterations = 100)
    {
        Guard.IsNotNull(source, nameof(source));

        return source.Select(x => x.ClosePrice).TryGetTrixDownAcceleration(out value, periods, iterations);
    }

    public static bool TryGetTrixDownAcceleration<T>(this IEnumerable<T> source, Func<T, decimal> selector, out TrixValue value, int periods = 9, int iterations = 100)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));

        return source.Select(selector).TryGetTrixDownAcceleration(out value, periods, iterations);
    }

    public static bool TryGetTrixDownAcceleration(this IEnumerable<decimal> source, out TrixValue value, int periods = 9, int iterations = 100)
    {
        Guard.IsNotNull(source, nameof(source));

        value = TrixValue.Empty;

        // the last value must not be in downtrend
        var last = source.Trix(periods).Last();
        if (last.IsRoC2Down)
        {
            return false;
        }

        // define the initial search range
        var high = source.Max() * 2M;
        var low = source.Min() / 2M;

        for (var i = 0; i < iterations; i++)
        {
            var candidatePrice = (low + high) / 2;

            // probe halfway between the range
            var candidateTrix = source.Append(candidatePrice).Trix(periods).Last();

            // keep the best candidate so far
            if (candidateTrix.IsRoC2Down)
            {
                value = candidateTrix;
            }

            // adjust ranges to search for a better candidate
            if (candidateTrix.IsRoC2Up)
            {
                high = candidatePrice;
            }
            else if (candidateTrix.IsRoC2Down)
            {
                low = candidatePrice;
            }
            else
            {
                value = candidateTrix;
                return true;
            }
        }

        return value != TrixValue.Empty;
    }
}