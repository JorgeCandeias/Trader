using Outcompute.Trader.Core.Mathematics;

namespace Outcompute.Trader.Indicators;

/// <summary>
/// Calculates the difference between the source series and its SMA.
/// </summary>
public class SmaDev : CompositeIndicator<decimal?, decimal?>
{
    public const int DefaultPeriods = 10;

    public SmaDev(IndicatorResult<decimal?> source, int periods = DefaultPeriods)
        : base(source, x =>
        {
            Guard.IsNotNull(source, nameof(source));
            Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

            return Indicator.Zip(
                Indicator.Sma(source, periods),
                Indicator.MovingWindow(source, periods),
                (m, w) =>
                {
                    if (w is null || m is null)
                    {
                        return null;
                    }

                    return w.Sum(x => MathN.Abs(x - m)) / periods;
                });
        })
    {
        Periods = periods;
    }

    public int Periods { get; }
}

public static partial class Indicator
{
    public static SmaDev SmaDev(this IndicatorResult<decimal?> source, int periods = Indicators.SmaDev.DefaultPeriods)
        => new(source, periods);

    public static IEnumerable<decimal?> ToSmaDev<T>(this IEnumerable<T> source, Func<T, decimal?> selector, int periods = Indicators.SmaDev.DefaultPeriods)
        => source.Select(selector).Identity().SmaDev(periods);

    public static IEnumerable<decimal?> ToSmaDev(this IEnumerable<decimal?> source, int periods = Indicators.SmaDev.DefaultPeriods)
        => source.ToSmaDev(x => x, periods);
}