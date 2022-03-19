using Outcompute.Trader.Core.Mathematics;

namespace Outcompute.Trader.Trading.Indicators;

public class Lowest : IndicatorBase<decimal?, decimal?>
{
    public Lowest(IndicatorResult<decimal?> source, int periods = 1, bool outputWarmup = false)
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
            highest = MathN.Min(Source[i], highest, MinMaxBehavior.NonNullWins);
        }

        return highest;
    }
}

public static partial class Indicator
{
    public static Lowest Lowest(this IndicatorResult<decimal?> source, int periods = 1, bool outputWarmup = false)
        => new(source, periods, outputWarmup);

    public static Lowest Lowest(this IndicatorResult<HL> source, int periods = 1, bool outputWarmup = false)
        => new(Transform(source, x => x.Low), periods, outputWarmup);

    public static Lowest Lowest(this IndicatorResult<HLC> source, int periods = 1, bool outputWarmup = false)
        => new(Transform(source, x => x.Low), periods, outputWarmup);

    public static IEnumerable<decimal?> ToLowest<T>(this IEnumerable<T> source, Func<T, decimal?> selector, int periods = 1, bool outputWarmup = false)
        => source.Select(selector).Identity().Lowest(periods, outputWarmup);

    public static IEnumerable<decimal?> ToLowest(this IEnumerable<Kline> source, int periods = 1, bool outputWarmup = false)
        => source.ToLowest(x => x.LowPrice, periods, outputWarmup);
}