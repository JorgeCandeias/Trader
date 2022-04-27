namespace Outcompute.Trader.Indicators;

public record struct BBP(decimal? BullPower, decimal? BearPower, decimal? Power)
{
    public static BBP Empty { get; } = new(null, null, null);
}

public class BullBearPower : CompositeIndicator<HLC, BBP>
{
    public const int DefaultPeriods = 13;

    public BullBearPower(IndicatorResult<HLC> source, int periods = DefaultPeriods)
        : base(source, x =>
        {
            Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

            var high = source.Transform(x => x.High);
            var low = source.Transform(x => x.Low);
            var close = source.Transform(x => x.Close);

            var ema = close.Ema(periods);
            var bull = Indicator.Zip(high, ema, (x, y) => x - y);
            var bear = Indicator.Zip(low, ema, (x, y) => x - y);

            return Indicator.Zip(bull, bear, (x, y) => new BBP(x, y, x + y));
        })
    {
    }
}

public static partial class Indicator
{
    public static BullBearPower BullBearPower(this IndicatorResult<HLC> source, int periods = Indicators.BullBearPower.DefaultPeriods)
        => new(source, periods);

    public static IEnumerable<BBP> ToBullBearPower<T>(this IEnumerable<T> source, Func<T, decimal?> highSelector, Func<T, decimal?> lowSelector, Func<T, decimal?> closeSelector, int periods = Indicators.BullBearPower.DefaultPeriods)
        => source.Select(x => new HLC(highSelector(x), lowSelector(x), closeSelector(x))).Identity().BullBearPower(periods);
}