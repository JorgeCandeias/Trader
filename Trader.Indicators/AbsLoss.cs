using Outcompute.Trader.Core.Mathematics;

namespace Outcompute.Trader.Indicators;

/// <summary>
/// Indicator that yields the absolute loss between the current value and the previous value.
/// </summary>
public class AbsLoss : IndicatorBase<decimal?, decimal?>
{
    public AbsLoss(IndicatorResult<decimal?> source) : base(source, true)
    {
        Ready();
    }

    protected override decimal? Calculate(int index)
    {
        if (index < 1)
        {
            return null;
        }

        return MathN.Abs(MathN.Min(Source[index] - Source[index - 1], 0));
    }
}

public static partial class Indicator
{
    public static AbsLoss AbsLoss(this IndicatorResult<decimal?> source)
        => new(source);

    public static IEnumerable<decimal?> ToAbsLoss(this IEnumerable<decimal?> source)
        => source.Identity().AbsLoss();
}