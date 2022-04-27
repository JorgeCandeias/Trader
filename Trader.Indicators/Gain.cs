using Outcompute.Trader.Core.Mathematics;

namespace Outcompute.Trader.Indicators;

/// <summary>
/// Calculates the gain between the current value and the previous value.
/// </summary>
public class Gain : IndicatorBase<decimal?, decimal?>
{
    public Gain(IndicatorResult<decimal?> source) : base(source, true)
    {
        Ready();
    }

    protected override decimal? Calculate(int index)
    {
        if (index < 1)
        {
            return null;
        }

        return MathN.Max(Source[index] - Source[index - 1], 0, MinMaxBehavior.NullWins);
    }
}

public static partial class Indicator
{
    public static Gain Gain(this IndicatorResult<decimal?> source)
        => new(source);

    public static IEnumerable<decimal?> ToGain<T>(this IEnumerable<T> source, Func<T, decimal?> selector)
        => source.Select(selector).Identity().Gain();

    public static IEnumerable<decimal?> ToGain(this IEnumerable<decimal?> source)
        => source.ToGain(x => x);
}