namespace Outcompute.Trader.Trading.Indicators;

public record struct DMI(decimal? Plus, decimal? Minus, decimal? Adx)
{
    public static DMI Empty { get; } = new DMI();
}

/// <summary>
/// Calculates the Directional Movement Index.
/// This comprises three series:
///     - Positive Directional Movement Index
///     - Negative Directional Movement Index
///     - Average Directional Movement Index
/// </summary>
public class Dmi : IndicatorBase<HLC, DMI>
{
    internal const int DefaultAdxPeriods = 14;
    internal const int DefaultDiPeriods = 14;

    private readonly Identity<HLC> _source;
    private readonly IIndicatorResult<DMI> _indicator;

    [SuppressMessage("Major Code Smell", "S3358:Ternary operators should not be nested", Justification = "N/A")]
    public Dmi(int adxPeriods = DefaultAdxPeriods, int diPeriods = DefaultDiPeriods)
    {
        Guard.IsGreaterThanOrEqualTo(adxPeriods, 1, nameof(adxPeriods));
        Guard.IsGreaterThanOrEqualTo(diPeriods, 1, nameof(diPeriods));

        AdxPeriods = adxPeriods;
        DiPeriods = diPeriods;

        _source = Indicator.Identity<HLC>();

        var high = Indicator.Transform(_source, x => x.High);
        var low = Indicator.Transform(_source, x => x.Low);

        var up = Indicator.Change(high);
        var down = -Indicator.Change(low);
        var plusDM = Indicator.Zip(up, down, (u, d) => u is null ? null : u > d && u > 0 ? u : 0);
        var minusDM = Indicator.Zip(up, down, (u, d) => d is null ? null : d > u && d > 0 ? d : 0);
        var atr = Indicator.Atr(_source, DiPeriods, AtrMethod.Rma);
        var plus = Indicator.FillNull(100M * Indicator.Rma(plusDM, DiPeriods) / atr);
        var minus = Indicator.FillNull(100M * Indicator.Rma(minusDM, DiPeriods) / atr);
        var adx = 100M * Indicator.Rma(Indicator.Abs(plus - minus) / Indicator.Transform(plus + minus, x => x == 0 ? 1 : x), AdxPeriods);

        _indicator = Indicator.Zip(plus, minus, adx, (p, m, x) => new DMI(p, m, x));
    }

    public Dmi(IIndicatorResult<HLC> source, int adxPeriods = DefaultAdxPeriods, int diPeriods = DefaultDiPeriods) : this(adxPeriods, diPeriods)
    {
        Guard.IsNotNull(source, nameof(source));

        LinkFrom(source);
    }

    public int AdxPeriods { get; }

    public int DiPeriods { get; }

    protected override DMI Calculate(int index)
    {
        // update the core source and cascade
        _source.Update(index, Source[index]);

        // return the final result
        return _indicator[index];
    }
}

public static partial class Indicator
{
    public static Dmi Dmi(int adxPeriods = Indicators.Dmi.DefaultAdxPeriods, int diPeriods = Indicators.Dmi.DefaultDiPeriods) => new(adxPeriods, diPeriods);

    public static Dmi Dmi(IIndicatorResult<HLC> source, int adxPeriods = Indicators.Dmi.DefaultAdxPeriods, int diPeriods = Indicators.Dmi.DefaultDiPeriods) => new(source, adxPeriods, diPeriods);
}

public static class DmiEnumerableExtensions
{
    public static IEnumerable<DMI> Dmi<T>(this IEnumerable<T> source, Func<T, decimal?> highSelector, Func<T, decimal?> lowSelector, Func<T, decimal?> closeSelector, int adxPeriods = Indicators.Dmi.DefaultAdxPeriods, int diPeriods = Indicators.Dmi.DefaultDiPeriods)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(highSelector, nameof(highSelector));
        Guard.IsNotNull(lowSelector, nameof(lowSelector));
        Guard.IsNotNull(closeSelector, nameof(closeSelector));

        using var indicator = Indicator.Dmi(adxPeriods, diPeriods);

        foreach (var item in source)
        {
            indicator.Add(new HLC(highSelector(item), lowSelector(item), closeSelector(item)));

            yield return indicator[^1];
        }
    }

    public static IEnumerable<DMI> Dmi(this IEnumerable<Kline> source, int adxLength = Indicators.Dmi.DefaultAdxPeriods, int diLength = Indicators.Dmi.DefaultDiPeriods)
    {
        return source.Dmi(x => x.HighPrice, x => x.LowPrice, x => x.ClosePrice, adxLength, diLength);
    }
}