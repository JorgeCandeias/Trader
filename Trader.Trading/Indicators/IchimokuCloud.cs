using static Outcompute.Trader.Trading.Indicators.Indicator;

namespace Outcompute.Trader.Trading.Indicators;

public record struct IchimokuCloudResult(decimal? ConversionLine, decimal? BaseLine, decimal? LeadLine1, decimal? LeadLine2)
{
    public static IchimokuCloudResult Empty { get; } = new IchimokuCloudResult();
}

public class IchimokuCloud : CompositeIndicator<HL, IchimokuCloudResult>
{
    internal const int DefaultConversionPeriods = 9;
    internal const int DefaultBasePeriods = 26;
    internal const int DefaultLaggingSpan2Periods = 52;
    internal const int DefaultDisplacement = 26;

    private static IndicatorResult<decimal?> Donchian(IndicatorResult<HL> source, int periods)
    {
        return Zip(
            Indicator.Highest(source, periods),
            Indicator.Lowest(source, periods),
            (x, y) => (x + y) / 2);
    }

    private static IndicatorResult<IchimokuCloudResult> Create(IndicatorResult<HL> source, int conversionPeriods, int basePeriods, int laggingSpan2Periods, int displacement)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThanOrEqualTo(conversionPeriods, 1, nameof(conversionPeriods));
        Guard.IsGreaterThanOrEqualTo(basePeriods, 1, nameof(basePeriods));
        Guard.IsGreaterThanOrEqualTo(laggingSpan2Periods, 1, nameof(laggingSpan2Periods));
        Guard.IsGreaterThanOrEqualTo(displacement, 1, nameof(displacement));

        var conversionLine = Donchian(source, conversionPeriods);
        var baseLine = Donchian(source, basePeriods);
        var leadLine1 = Zip(conversionLine, baseLine, (x, y) => (x + y) / 2);
        var leadLine2 = Donchian(source, laggingSpan2Periods);

        return Zip(conversionLine, baseLine, leadLine1, leadLine2, (a, b, c, d) => new IchimokuCloudResult(a, b, c, d));
    }

    public IchimokuCloud(IndicatorResult<HL> source, int conversionPeriods = DefaultConversionPeriods, int basePeriods = DefaultBasePeriods, int laggingSpan2Periods = DefaultLaggingSpan2Periods, int displacement = DefaultDisplacement)
        : base(source, x => Create(x, conversionPeriods, basePeriods, laggingSpan2Periods, displacement))
    {
        ConversionPeriods = conversionPeriods;
        BasePeriods = basePeriods;
        LaggingSpan2Periods = laggingSpan2Periods;
        Displacement = displacement;
    }

    public int ConversionPeriods { get; }
    public int BasePeriods { get; }
    public int LaggingSpan2Periods { get; }
    public int Displacement { get; }
}

public static partial class Indicator
{
    public static IchimokuCloud IchimokuCloud(
        this IndicatorResult<HL> source,
        int conversionPeriods = Indicators.IchimokuCloud.DefaultConversionPeriods,
        int basePeriods = Indicators.IchimokuCloud.DefaultBasePeriods,
        int laggingSpan2Periods = Indicators.IchimokuCloud.DefaultLaggingSpan2Periods,
        int displacement = Indicators.IchimokuCloud.DefaultDisplacement)
    {
        return new IchimokuCloud(source, conversionPeriods, basePeriods, laggingSpan2Periods, displacement);
    }

    public static IEnumerable<IchimokuCloudResult> ToIchimokuCloud<T>(this IEnumerable<T> source, Func<T, decimal?> highSelector, Func<T, decimal?> lowSelector, int conversionPeriods = 9, int basePeriods = 26, int laggingSpan2Periods = 52, int displacement = 26)
        => source.Select(x => new HL(highSelector(x), lowSelector(x))).Identity().IchimokuCloud(conversionPeriods, basePeriods, laggingSpan2Periods, displacement);

    public static IEnumerable<IchimokuCloudResult> ToIchimokuCloud(this IEnumerable<Kline> source, int conversionPeriods = 9, int basePeriods = 26, int laggingSpan2Periods = 52, int displacement = 26)
        => source.ToIchimokuCloud(x => x.HighPrice, x => x.LowPrice, conversionPeriods, basePeriods, laggingSpan2Periods, displacement);
}