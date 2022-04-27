using Outcompute.Trader.Core.Mathematics;

namespace Outcompute.Trader.Indicators;

public class Log : Transform<decimal?, decimal?>
{
    public Log(IndicatorResult<decimal?> source)
        : base(source, x => MathN.Log(x))
    {
    }
}

public static partial class Indicator
{
    public static Log Log(this IndicatorResult<decimal?> source)
        => new(source);

    public static IEnumerable<decimal?> ToLog(this IEnumerable<decimal?> source)
        => source.Identity().Log();
}