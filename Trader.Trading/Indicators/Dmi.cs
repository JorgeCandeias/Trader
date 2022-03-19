using static Outcompute.Trader.Trading.Indicators.Indicator;

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
public class Dmi : CompositeIndicator<HLC, DMI>
{
    internal const int DefaultAdxPeriods = 14;
    internal const int DefaultDiPeriods = 14;

    public Dmi(IndicatorResult<HLC> source, int adxPeriods = DefaultAdxPeriods, int diPeriods = DefaultDiPeriods)
        : base(source, x =>
        {
            Guard.IsNotNull(source, nameof(source));
            Guard.IsGreaterThanOrEqualTo(adxPeriods, 1, nameof(adxPeriods));
            Guard.IsGreaterThanOrEqualTo(diPeriods, 1, nameof(diPeriods));

            var high = source.Transform(x => x.High);
            var low = source.Transform(x => x.Low);

            var up = high.Change();
            var down = -low.Change();
            var plusDM = Zip(up, down, (u, d) =>
            {
                if (u is null)
                {
                    return null;
                }

                return u > d && u > 0 ? u : 0;
            });
            var minusDM = Zip(up, down, (u, d) =>
            {
                if (d is null)
                {
                    return null;
                }

                return d > u && d > 0 ? d : 0;
            });
            var atr = Indicator.Rma(Indicator.TrueRange(source), diPeriods);
            var plus = Indicator.FillNull(100M * Indicator.Rma(plusDM, diPeriods) / atr);
            var minus = Indicator.FillNull(100M * Indicator.Rma(minusDM, diPeriods) / atr);
            var adx = 100M * Indicator.Rma(Indicator.Abs(plus - minus) / Indicator.Transform(plus + minus, x => x == 0 ? 1 : x), adxPeriods);

            return Zip(plus, minus, adx, (p, m, x) => new DMI(p, m, x));
        })
    {
    }
}

public static partial class Indicator
{
    public static Dmi Dmi(this IndicatorResult<HLC> source, int adxPeriods = Indicators.Dmi.DefaultAdxPeriods, int diPeriods = Indicators.Dmi.DefaultDiPeriods)
        => new(source, adxPeriods, diPeriods);

    public static IEnumerable<DMI> ToDmi<T>(this IEnumerable<T> source, Func<T, decimal?> highSelector, Func<T, decimal?> lowSelector, Func<T, decimal?> closeSelector, int adxPeriods = Indicators.Dmi.DefaultAdxPeriods, int diPeriods = Indicators.Dmi.DefaultDiPeriods)
        => source.Select(x => new HLC(highSelector(x), lowSelector(x), closeSelector(x))).Identity().Dmi(adxPeriods, diPeriods);

    public static IEnumerable<DMI> ToDmi(this IEnumerable<Kline> source, int adxPeriods = Indicators.Dmi.DefaultAdxPeriods, int diPeriods = Indicators.Dmi.DefaultDiPeriods)
        => ToDmi(source, x => x.HighPrice, x => x.LowPrice, x => x.ClosePrice, adxPeriods, diPeriods);
}