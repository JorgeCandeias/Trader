namespace System.Collections.Generic;

public record TrixValue
{
    /// <summary>
    /// The price from the underlying source sequence.
    /// </summary>
    public decimal Price { get; init; }

    /// <summary>
    /// The triple exponential moving average from the underlying source.
    /// </summary>
    public decimal Ema3 { get; init; }

    /// <summary>
    /// The triple exponential moving average from the natural logarithm of the underlying source.
    /// </summary>
    public decimal LogEma3 { get; init; }

    /// <summary>
    /// The rate of change of <see cref="LogEma3"/>, first derivative of <see cref="LogEma3"/>.
    /// </summary>
    public decimal Velocity { get; init; }

    /// <summary>
    /// The rate of change of <see cref="Velocity"/>, second derivative of <see cref="LogEma3"/>.
    /// </summary>
    public decimal Acceleration { get; init; }

    /// <summary>
    /// The rate of change of <see cref="Acceleration"/>, third derivative of <see cref="LogEma3"/>.
    /// </summary>
    public decimal Jerk { get; init; }

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

        var priceEnumerator = source.Select(selector).GetEnumerator();
        var trixEnumerator = source.Select(selector).Ema(periods).Ema(periods).Ema(periods).GetEnumerator();
        var emaEnumerator = source.Select(selector).Log().Ema(periods).Ema(periods).Ema(periods).GetEnumerator();
        var velocityEnumerator = source.Select(selector).Log().Ema(periods).Ema(periods).Ema(periods).Change().GetEnumerator();
        var accelerationEnumerator = source.Select(selector).Log().Ema(periods).Ema(periods).Ema(periods).Change().Change().GetEnumerator();
        var jerkEnumerator = source.Select(selector).Log().Ema(periods).Ema(periods).Ema(periods).Change().Change().Change().GetEnumerator();

        while (priceEnumerator.MoveNext() && trixEnumerator.MoveNext() && emaEnumerator.MoveNext() && velocityEnumerator.MoveNext() && accelerationEnumerator.MoveNext() && jerkEnumerator.MoveNext())
        {
            yield return new TrixValue
            {
                Price = priceEnumerator.Current,
                Ema3 = trixEnumerator.Current,
                LogEma3 = emaEnumerator.Current,
                Velocity = velocityEnumerator.Current,
                Acceleration = accelerationEnumerator.Current,
                Jerk = jerkEnumerator.Current
            };
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
        if (last.Acceleration >= 0)
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

            // adjust ranges to search for a better candidate
            if (candidateTrix.Acceleration > 0)
            {
                value = candidateTrix;
                high = candidatePrice;
            }
            else if (candidateTrix.Acceleration < 0)
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
        if (last.Acceleration <= 0)
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

            // adjust ranges to search for a better candidate
            if (candidateTrix.Acceleration > 0)
            {
                high = candidatePrice;
            }
            else if (candidateTrix.Acceleration < 0)
            {
                value = candidateTrix;
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
        if (last.Jerk >= 0)
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
            if (candidateTrix.Jerk > 0)
            {
                value = candidateTrix;
            }

            // adjust ranges to search for a better candidate
            if (candidateTrix.Jerk > 0)
            {
                high = candidatePrice;
            }
            else if (candidateTrix.Jerk < 0)
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
        if (last.Jerk <= 0)
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
            if (candidateTrix.Jerk <= 0)
            {
                value = candidateTrix;
            }

            // adjust ranges to search for a better candidate
            if (candidateTrix.Jerk > 0)
            {
                high = candidatePrice;
            }
            else if (candidateTrix.Jerk < 0)
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