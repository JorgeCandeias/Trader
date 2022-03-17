using Outcompute.Trader.Core.Mathematics;

namespace Outcompute.Trader.Trading.Indicators;

public class Log : Transform<decimal?, decimal?>
{
    public Log() : base(Transform)
    {
    }

    public Log(IIndicatorResult<decimal?> source) : base(source, Transform)
    {
    }

    private static readonly Func<decimal?, decimal?> Transform = x => MathN.Log(x);
}

public static partial class Indicator
{
    public static Log Log() => new();

    public static Log Log(IIndicatorResult<decimal?> source) => new(source);
}

public static class LogEnumerableExtensions
{
    public static IEnumerable<decimal?> Log(this IEnumerable<decimal?> source)
    {
        Guard.IsNotNull(source, nameof(source));

        using var indicator = Indicator.Log();

        foreach (var item in source)
        {
            indicator.Add(item);

            yield return indicator[^1];
        }
    }
}