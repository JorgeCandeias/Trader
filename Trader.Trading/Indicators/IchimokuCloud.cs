namespace Outcompute.Trader.Trading.Indicators;

public record struct IchimokuCloudResult(decimal? ConversionLine, decimal? BaseLine, decimal? LeadLine1, decimal? LeadLine2)
{
    public static IchimokuCloudResult Empty { get; } = new IchimokuCloudResult();
}

public class IchimokuCloud : IndicatorBase<HL, IchimokuCloudResult>
{
    internal const int DefaultConversionPeriods = 9;
    internal const int DefaultBasePeriods = 26;
    internal const int DefaultLaggingSpan2Periods = 52;
    internal const int DefaultDisplacement = 26;

    private readonly Identity<HL> _source;
    private readonly IIndicatorResult<IchimokuCloudResult> _result;

    public IchimokuCloud(int conversionPeriods = DefaultConversionPeriods, int basePeriods = DefaultBasePeriods, int laggingSpan2Periods = DefaultLaggingSpan2Periods, int displacement = DefaultDisplacement)
    {
        Guard.IsGreaterThanOrEqualTo(conversionPeriods, 1, nameof(conversionPeriods));
        Guard.IsGreaterThanOrEqualTo(basePeriods, 1, nameof(basePeriods));
        Guard.IsGreaterThanOrEqualTo(laggingSpan2Periods, 1, nameof(laggingSpan2Periods));
        Guard.IsGreaterThanOrEqualTo(displacement, 1, nameof(displacement));

        ConversionPeriods = conversionPeriods;
        BasePeriods = basePeriods;
        LaggingSpan2Periods = laggingSpan2Periods;
        Displacement = displacement;

        _source = Indicator.Identity<HL>();

        IIndicatorResult<decimal?> Donchian(int periods) => Indicator.Zip(
            Indicator.Highest(_source, periods),
            Indicator.Lowest(_source, periods),
            (x, y) => (x + y) / 2);

        var conversionLine = Donchian(conversionPeriods);
        var baseLine = Donchian(basePeriods);
        var leadLine1 = Indicator.Zip(conversionLine, baseLine, (x, y) => (x + y) / 2);
        var leadLine2 = Donchian(laggingSpan2Periods);

        _result = Indicator.Zip(conversionLine, baseLine, leadLine1, leadLine2, (a, b, c, d) => new IchimokuCloudResult(a, b, c, d));
    }

    public IchimokuCloud(IIndicatorResult<HL> source, int conversionPeriods = DefaultConversionPeriods, int basePeriods = DefaultBasePeriods, int laggingSpan2Periods = DefaultLaggingSpan2Periods, int displacement = DefaultDisplacement)
        : this(conversionPeriods, basePeriods, laggingSpan2Periods, displacement)
    {
        Guard.IsNotNull(source, nameof(source));

        LinkFrom(source);
    }

    public int ConversionPeriods { get; }
    public int BasePeriods { get; }
    public int LaggingSpan2Periods { get; }
    public int Displacement { get; }

    protected override IchimokuCloudResult Calculate(int index)
    {
        // update the core source and cascade
        _source.Update(index, Source[index]);

        // return the final result
        return _result[index];
    }
}

public static partial class Indicator
{
    public static IchimokuCloud IchimokuCloud(
        int conversionPeriods = Indicators.IchimokuCloud.DefaultConversionPeriods,
        int basePeriods = Indicators.IchimokuCloud.DefaultBasePeriods,
        int laggingSpan2Periods = Indicators.IchimokuCloud.DefaultLaggingSpan2Periods,
        int displacement = Indicators.IchimokuCloud.DefaultDisplacement)
    {
        return new IchimokuCloud(conversionPeriods, basePeriods, laggingSpan2Periods, displacement);
    }

    public static IchimokuCloud IchimokuCloud(
        IIndicatorResult<HL> source,
        int conversionPeriods = Indicators.IchimokuCloud.DefaultConversionPeriods,
        int basePeriods = Indicators.IchimokuCloud.DefaultBasePeriods,
        int laggingSpan2Periods = Indicators.IchimokuCloud.DefaultLaggingSpan2Periods,
        int displacement = Indicators.IchimokuCloud.DefaultDisplacement)
    {
        return new IchimokuCloud(source, conversionPeriods, basePeriods, laggingSpan2Periods, displacement);
    }
}

public static class IchimokuCloudEnumerableExtensions
{
    public static IEnumerable<IchimokuCloudResult> IchimokuCloud<T>(this IEnumerable<T> source, Func<T, decimal?> highSelector, Func<T, decimal?> lowSelector, int conversionPeriods = 9, int basePeriods = 26, int laggingSpan2Periods = 52, int displacement = 26)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(highSelector, nameof(highSelector));
        Guard.IsNotNull(lowSelector, nameof(lowSelector));

        using var indicator = Indicator.IchimokuCloud(conversionPeriods, basePeriods, laggingSpan2Periods, displacement);

        foreach (var item in source)
        {
            indicator.Add(new HL(highSelector(item), lowSelector(item)));

            yield return indicator[^1];
        }
    }

    public static IEnumerable<IchimokuCloudResult> IchimokuCloud(this IEnumerable<Kline> source, int conversionPeriods = 9, int basePeriods = 26, int laggingSpan2Periods = 52, int displacement = 26)
    {
        return source.IchimokuCloud(x => x.HighPrice, x => x.LowPrice, conversionPeriods, basePeriods, laggingSpan2Periods, displacement);
    }
}