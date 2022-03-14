namespace Outcompute.Trader.Trading.Indicators;

public record struct IchimokuCloud(decimal? ConversionLine, decimal? BaseLine, decimal? LeadLine1, decimal? LeadLine2, decimal? LaggingSpan, decimal? LeadingSpanA, decimal? LeadingSpanB);

public static class IchimokuCloudExtensions
{
    public static IEnumerable<IchimokuCloud> IchimokuCloud(this IEnumerable<(decimal? High, decimal? Low, decimal? Close)> source, int conversionPeriods = 9, int basePeriods = 26, int laggingSpan2Periods = 52, int displacement = 26)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThanOrEqualTo(conversionPeriods, 1, nameof(conversionPeriods));
        Guard.IsGreaterThanOrEqualTo(basePeriods, 1, nameof(basePeriods));
        Guard.IsGreaterThanOrEqualTo(laggingSpan2Periods, 1, nameof(laggingSpan2Periods));
        Guard.IsGreaterThanOrEqualTo(displacement, 1, nameof(displacement));

        IEnumerable<decimal?> Donchian(int length) => source
            .HighestLowest(x => x.High, x => x.Low, length)
            .Select(x => (x.Lowest + x.Highest) / 2);

        var conversionLines = Donchian(conversionPeriods).ToList();
        var baseLines = Donchian(basePeriods).ToList();
        var leadLines1 = conversionLines.Zip(baseLines).Select(x => (x.First + x.Second) / 2).ToList();
        var leadLines2 = Donchian(laggingSpan2Periods).ToList();

        var laggingSpans = source.Skip(displacement - 1).Select(x => x.Close).Concat(Enumerable.Repeat<decimal?>(null, displacement - 1));
        var leadingSpanA = Enumerable.Repeat<decimal?>(null, displacement - 1).Concat(leadLines1);
        var leadingSpanB = Enumerable.Repeat<decimal?>(null, displacement - 1).Concat(leadLines2);

        return conversionLines
            .Zip(baseLines, (x, y) => (ConversionLine: x, BaseLine: y))
            .Zip(leadLines1, (x, y) => (x.ConversionLine, x.BaseLine, LeadLine1: y))
            .Zip(leadLines2, (x, y) => (x.ConversionLine, x.BaseLine, x.LeadLine1, LeadLine2: y))
            .Zip(laggingSpans, (x, y) => (x.ConversionLine, x.BaseLine, x.LeadLine1, x.LeadLine2, LaggingSpan: y))
            .Zip(leadingSpanA, (x, y) => (x.ConversionLine, x.BaseLine, x.LeadLine1, x.LeadLine2, x.LaggingSpan, LeadingSpanA: y))
            .Zip(leadingSpanB, (x, y) => (x.ConversionLine, x.BaseLine, x.LeadLine1, x.LeadLine2, x.LaggingSpan, x.LeadingSpanA, LeadingSpanB: y))
            .Select(x => new IchimokuCloud(x.ConversionLine, x.BaseLine, x.LeadLine1, x.LeadLine2, x.LaggingSpan, x.LeadingSpanA, x.LeadingSpanB));
    }

    public static IEnumerable<IchimokuCloud> IchimokuCloud<T>(this IEnumerable<T> source, Func<T, decimal?> highSelector, Func<T, decimal?> lowSelector, Func<T, decimal?> closeSelector, int conversionPeriods = 9, int basePeriods = 26, int laggingSpan2Periods = 52, int displacement = 26)
    {
        return source.Select(x => (highSelector(x), lowSelector(x), closeSelector(x))).IchimokuCloud(conversionPeriods, basePeriods, laggingSpan2Periods, displacement);
    }

    public static IEnumerable<IchimokuCloud> IchimokuCloud(this IEnumerable<Kline> source, int conversionPeriods = 9, int basePeriods = 26, int laggingSpan2Periods = 52, int displacement = 26)
    {
        return source.IchimokuCloud(x => x.HighPrice, x => x.LowPrice, x => x.ClosePrice, conversionPeriods, basePeriods, laggingSpan2Periods, displacement);
    }
}