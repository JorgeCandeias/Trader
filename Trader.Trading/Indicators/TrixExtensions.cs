using Outcompute.Trader.Trading.Indicators;

namespace System.Collections.Generic;

public record TrixValue
{
    /// <summary>
    /// The price from the underlying source sequence.
    /// </summary>
    public decimal? Price { get; init; }

    /// <summary>
    /// The triple exponential moving average from the underlying source.
    /// </summary>
    public decimal? Ema3 { get; init; }

    /// <summary>
    /// The rate of change of <see cref="LogEma3"/>, first derivative of <see cref="LogEma3"/>.
    /// </summary>
    public decimal? Velocity { get; init; }

    /// <summary>
    /// The rate of change of <see cref="Velocity"/>, second derivative of <see cref="LogEma3"/>.
    /// </summary>
    public decimal? Acceleration { get; init; }

    /// <summary>
    /// The rate of change of <see cref="Acceleration"/>, third derivative of <see cref="LogEma3"/>.
    /// </summary>
    public decimal? Jerk { get; init; }

    public static TrixValue Empty { get; } = new TrixValue();
}

public static class TrixExtensions
{
    public static IEnumerable<TrixValue> Trix(this IEnumerable<decimal?> source, int periods = 9)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThan(periods, 0, nameof(periods));

        var priceEnumerator = source.GetEnumerator();
        var trixEnumerator = source.ToEma(periods).ToEma(periods).ToEma(periods).GetEnumerator();
        var velocityEnumerator = source.ToLog().ToEma(periods).ToEma(periods).ToEma(periods).ToChange().GetEnumerator();
        var accelerationEnumerator = source.ToLog().ToEma(periods).ToEma(periods).ToEma(periods).ToChange().ToChange().GetEnumerator();
        var jerkEnumerator = source.ToLog().ToEma(periods).ToEma(periods).ToEma(periods).ToChange().ToChange().ToChange().GetEnumerator();

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

    public static IEnumerable<TrixValue> Trix(this IEnumerable<Kline> source, int periods = 9)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThan(periods, 0, nameof(periods));

        return source.Select(x => (decimal?)x.ClosePrice).Trix(periods);
    }
}