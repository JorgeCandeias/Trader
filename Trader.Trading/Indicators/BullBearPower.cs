namespace Outcompute.Trader.Trading.Indicators;

public record struct BBP(decimal? BullPower, decimal? BearPower, decimal? Power)
{
    public static BBP Empty { get; } = new(null, null, null);
}

public class BullBearPower : IndicatorBase<HLC, BBP>
{
    internal const int DefaultPeriods = 13;

    private readonly Identity<HLC> _source;
    private readonly IIndicatorResult<BBP> _indicator;

    public BullBearPower(int periods = DefaultPeriods)
    {
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        _source = Indicator.Identity<HLC>();

        var high = Indicator.Transform(_source, x => x.High);
        var low = Indicator.Transform(_source, x => x.Low);
        var close = Indicator.Transform(_source, x => x.Close);

        var ema = Indicator.Ema(close, periods);
        var bull = Indicator.Zip(high, ema, (x, y) => x - y);
        var bear = Indicator.Zip(low, ema, (x, y) => x - y);

        _indicator = Indicator.Zip(bull, bear, (x, y) => new BBP(x, y, x + y));
    }

    public BullBearPower(IIndicatorResult<HLC> source, int periods = DefaultPeriods) : this(periods)
    {
        Guard.IsNotNull(source, nameof(source));

        LinkFrom(source);
    }

    protected override BBP Calculate(int index)
    {
        // update the core source and cascade
        _source.Update(index, Source[index]);

        // return the final result
        return _indicator[index];
    }
}

public static partial class Indicator
{
    public static BullBearPower BullBearPower(int periods = Indicators.BullBearPower.DefaultPeriods) => new(periods);

    public static BullBearPower BullBearPower(IIndicatorResult<HLC> source, int periods = Indicators.BullBearPower.DefaultPeriods) => new(source, periods);
}

public static class BullBearPowerEnumerableExtensions
{
    public static IEnumerable<BBP> BullBearPower<T>(this IEnumerable<T> source, Func<T, decimal?> highSelector, Func<T, decimal?> lowSelector, Func<T, decimal?> closeSelector, int periods = Indicators.BullBearPower.DefaultPeriods)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(highSelector, nameof(highSelector));
        Guard.IsNotNull(lowSelector, nameof(lowSelector));
        Guard.IsNotNull(closeSelector, nameof(closeSelector));

        using var indicator = Indicator.BullBearPower(periods);

        foreach (var item in source)
        {
            indicator.Add(new HLC(highSelector(item), lowSelector(item), closeSelector(item)));

            yield return indicator[^1];
        }
    }

    public static IEnumerable<BBP> BullBearPower(this IEnumerable<Kline> source, int periods = Indicators.BullBearPower.DefaultPeriods)
    {
        return source.BullBearPower(x => x.HighPrice, x => x.LowPrice, x => x.ClosePrice, periods);
    }
}