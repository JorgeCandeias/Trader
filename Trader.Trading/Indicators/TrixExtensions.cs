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
        var velocityEnumerator = source.Select(selector).Log().Ema(periods).Ema(periods).Ema(periods).Change().GetEnumerator();
        var accelerationEnumerator = source.Select(selector).Log().Ema(periods).Ema(periods).Ema(periods).Change().Change().GetEnumerator();
        var jerkEnumerator = source.Select(selector).Log().Ema(periods).Ema(periods).Ema(periods).Change().Change().Change().GetEnumerator();

        while (priceEnumerator.MoveNext() && trixEnumerator.MoveNext() && velocityEnumerator.MoveNext() && accelerationEnumerator.MoveNext() && jerkEnumerator.MoveNext())
        {
            yield return new TrixValue
            {
                Price = priceEnumerator.Current,
                Ema3 = trixEnumerator.Current,
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

    public static bool TryGetTrixUpJerk(this IEnumerable<Kline> source, out TrixValue value, int periods = 9, decimal jerk = 0, int iterations = 100)
    {
        Guard.IsNotNull(source, nameof(source));

        return source.Select(x => x.ClosePrice).TryGetTrixUpAcceleration(out value, periods, jerk, iterations);
    }

    public static bool TryGetTrixUpAcceleration<T>(this IEnumerable<T> source, Func<T, decimal> selector, out TrixValue value, int periods = 9, decimal jerk = 0, int iterations = 100)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));

        return source.Select(selector).TryGetTrixUpAcceleration(out value, periods, jerk, iterations);
    }

    public static bool TryGetTrixUpAcceleration(this IEnumerable<decimal> source, out TrixValue value, int periods = 9, decimal jerk = 0, int iterations = 100)
    {
        Guard.IsNotNull(source, nameof(source));

        value = TrixValue.Empty;

        // the last value must not be in uptrend
        var last = source.Trix(periods).Last();
        if (last.Jerk >= jerk)
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
            if (candidateTrix.Jerk > jerk)
            {
                high = candidatePrice;
                value = candidateTrix;
            }
            else if (candidateTrix.Jerk < jerk)
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

    public static bool TryGetTrixDownJerk(this IEnumerable<Kline> source, out TrixValue value, int periods = 9, int iterations = 100)
    {
        Guard.IsNotNull(source, nameof(source));

        return source.Select(x => x.ClosePrice).TryGetTrixDownJerk(out value, periods, iterations);
    }

    public static bool TryGetTrixDownJerk<T>(this IEnumerable<T> source, Func<T, decimal> selector, out TrixValue value, int periods = 9, int iterations = 100)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));

        return source.Select(selector).TryGetTrixDownJerk(out value, periods, iterations);
    }

    public static bool TryGetTrixDownJerk(this IEnumerable<decimal> source, out TrixValue value, int periods = 9, int iterations = 100)
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

    public static bool TryGetTrixUpVelocity(this IEnumerable<Kline> source, out TrixValue value, int periods = 9, decimal velocity = 0, int iterations = 100)
    {
        Guard.IsNotNull(source, nameof(source));

        return source.Select(x => x.ClosePrice).TryGetTrixUpVelocity(out value, periods, velocity, iterations);
    }

    public static bool TryGetTrixUpVelocity<T>(this IEnumerable<T> source, Func<T, decimal> selector, out TrixValue value, int periods = 9, decimal velocity = 0, int iterations = 100)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));

        return source.Select(selector).TryGetTrixUpVelocity(out value, periods, velocity, iterations);
    }

    public static bool TryGetTrixUpVelocity(this IEnumerable<decimal> source, out TrixValue value, int periods = 9, decimal velocity = 0, int iterations = 100)
    {
        Guard.IsNotNull(source, nameof(source));

        value = TrixValue.Empty;

        // the last value must be in downtrend
        var last = source.Trix(periods).Last();
        if (last.Velocity >= velocity)
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
            if (candidateTrix.Velocity > velocity)
            {
                value = candidateTrix;
                high = candidatePrice;
            }
            else if (candidateTrix.Velocity < velocity)
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

    public static bool TryGetTrixDownVelocity(this IEnumerable<Kline> source, out TrixValue value, int periods = 9, int iterations = 100)
    {
        Guard.IsNotNull(source, nameof(source));

        return source.Select(x => x.ClosePrice).TryGetTrixDownVelocity(out value, periods, iterations);
    }

    public static bool TryGetTrixDownVelocity<T>(this IEnumerable<T> source, Func<T, decimal> selector, out TrixValue value, int periods = 9, int iterations = 100)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));

        return source.Select(selector).TryGetTrixDownVelocity(out value, periods, iterations);
    }

    public static bool TryGetTrixDownVelocity(this IEnumerable<decimal> source, out TrixValue value, int periods = 9, int iterations = 100)
    {
        Guard.IsNotNull(source, nameof(source));

        value = TrixValue.Empty;

        // the last value must be in downtrend
        var last = source.Trix(periods).Last();
        if (last.Velocity <= 0)
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
            if (candidateTrix.Velocity > 0)
            {
                high = candidatePrice;
            }
            else if (candidateTrix.Velocity < 0)
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

    public static bool TryGetTrixUpVelocityAndAcceleration(this IEnumerable<Kline> source, out TrixValue value, int periods = 9, decimal velocity = 0, decimal acceleration = 0, int iterations = 100)
    {
        Guard.IsNotNull(source, nameof(source));

        return source.Select(x => x.ClosePrice).TryGetTrixUpVelocityAndAcceleration(out value, periods, velocity, acceleration, iterations);
    }

    public static bool TryGetTrixUpVelocityAndAcceleration<T>(this IEnumerable<T> source, Func<T, decimal> selector, out TrixValue value, int periods = 9, decimal velocity = 0, decimal acceleration = 0, int iterations = 100)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));

        return source.Select(selector).TryGetTrixUpVelocityAndAcceleration(out value, periods, velocity, acceleration, iterations);
    }

    public static bool TryGetTrixUpVelocityAndAcceleration(this IEnumerable<decimal> source, out TrixValue value, int periods = 9, decimal velocity = 0, decimal acceleration = 0, int iterations = 100)
    {
        Guard.IsNotNull(source, nameof(source));

        value = TrixValue.Empty;

        // the last value must be in downtrend
        var last = source.Trix(periods).Last();
        if (last.Velocity >= velocity && last.Acceleration >= acceleration)
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
            if (candidateTrix.Velocity >= velocity && candidateTrix.Acceleration >= acceleration)
            {
                value = candidateTrix;
                high = candidatePrice;
            }
            else
            {
                low = candidatePrice;
            }
        }

        return value != TrixValue.Empty;
    }

    public static bool TryGetTrixDownVelocityOrAcceleration(this IEnumerable<Kline> source, out TrixValue value, int periods = 9, decimal velocity = 0, decimal acceleration = 0, int iterations = 100)
    {
        Guard.IsNotNull(source, nameof(source));

        return source.Select(x => x.ClosePrice).TryGetTrixDownVelocityOrAcceleration(out value, periods, velocity, acceleration, iterations);
    }

    public static bool TryGetTrixDownVelocityOrAcceleration<T>(this IEnumerable<T> source, Func<T, decimal> selector, out TrixValue value, int periods = 9, decimal velocity = 0, decimal acceleration = 0, int iterations = 100)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));

        return source.Select(selector).TryGetTrixDownVelocityOrAcceleration(out value, periods, velocity, acceleration, iterations);
    }

    public static bool TryGetTrixDownVelocityOrAcceleration(this IEnumerable<decimal> source, out TrixValue value, int periods = 9, decimal velocity = 0, decimal acceleration = 0, int iterations = 100)
    {
        Guard.IsNotNull(source, nameof(source));

        value = TrixValue.Empty;

        // the last value must be in downtrend
        var last = source.Trix(periods).Last();
        if (last.Velocity <= velocity || last.Acceleration <= acceleration)
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
            if (candidateTrix.Velocity >= velocity && candidateTrix.Acceleration >= acceleration)
            {
                high = candidatePrice;
            }
            else
            {
                low = candidatePrice;
                value = candidateTrix;
            }
        }

        return value != TrixValue.Empty;
    }
}