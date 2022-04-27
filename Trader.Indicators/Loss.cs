using Outcompute.Trader.Core.Mathematics;

namespace Outcompute.Trader.Indicators;

/// <summary>
/// Calculates the loss between the current value and the previous value.
/// </summary>
public class Loss : IndicatorBase<decimal?, decimal?>
{
    public Loss(IndicatorResult<decimal?> source) : base(source, true)
    {
        Ready();
    }

    protected override decimal? Calculate(int index)
    {
        if (index < 1)
        {
            return null;
        }

        return MathN.Min(Source[index] - Source[index - 1], 0, MinMaxBehavior.NullWins);
    }
}

public static partial class Indicator
{
    public static Loss Loss(this IndicatorResult<decimal?> source) => new(source);

    public static IEnumerable<decimal?> ToLoss(this IEnumerable<decimal?> source)
        => source.Identity().Loss();

    public static IEnumerable<decimal?> Loss<T>(this IEnumerable<T> source, Func<T, decimal?> selector)
        => source.Select(selector).Identity().Loss();
}