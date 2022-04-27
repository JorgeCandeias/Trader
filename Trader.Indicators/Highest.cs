using Outcompute.Trader.Core.Mathematics;

namespace Outcompute.Trader.Indicators;

public class Highest : IndicatorBase<decimal?, decimal?>
{
    public Highest(IndicatorResult<decimal?> source, int periods = 1, bool outputWarmup = false)
        : base(source, true)
    {
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        Periods = periods;
        OutputWarmup = outputWarmup;

        Ready();
    }

    public int Periods { get; }

    public bool OutputWarmup { get; }

    protected override decimal? Calculate(int index)
    {
        if (index < Periods - 1 && !OutputWarmup)
        {
            return null;
        }

        decimal? highest = null;
        for (var i = Math.Max(index - Periods + 1, 0); i <= index; i++)
        {
            highest = MathN.Max(Source[i], highest, MinMaxBehavior.NonNullWins);
        }

        return highest;
    }
}

public static partial class Indicator
{
    public static Highest Highest(this IndicatorResult<decimal?> source, int periods = 1, bool outputWarmup = false)
        => new(source, periods, outputWarmup);

    public static Highest Highest(this IndicatorResult<HL> source, int periods = 1, bool outputWarmup = false)
        => new(Transform(source, x => x.High), periods, outputWarmup);

    public static Highest Highest(this IndicatorResult<HLC> source, int periods = 1, bool outputWarmup = false)
        => new(Transform(source, x => x.High), periods, outputWarmup);

    public static IEnumerable<decimal?> ToHighest<T>(this IEnumerable<T> source, Func<T, decimal?> selector, int periods = 1, bool outputWarmup = false)
        => source.Select(selector).Identity().Highest(periods, outputWarmup);

    public static IEnumerable<decimal?> ToHighest(this IEnumerable<decimal?> source, int periods = 1, bool outputWarmup = false)
        => source.ToHighest(x => x, periods, outputWarmup);
}