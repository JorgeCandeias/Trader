﻿namespace Outcompute.Trader.Trading.Indicators;

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

public static class AverageDirectionalIndexExtensions
{
    public static IEnumerable<DMI> AverageDirectionalIndex<T>(this IEnumerable<T> source, Func<T, decimal?> highSelector, Func<T, decimal?> lowSelector, Func<T, decimal?> closeSelector, int adxLength = 14, int diLength = 14)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(highSelector, nameof(highSelector));
        Guard.IsNotNull(lowSelector, nameof(lowSelector));
        Guard.IsNotNull(closeSelector, nameof(closeSelector));
        Guard.IsGreaterThanOrEqualTo(adxLength, 1, nameof(adxLength));
        Guard.IsGreaterThanOrEqualTo(diLength, 1, nameof(diLength));

        var up = source.Select(highSelector).Change();
        var down = source.Select(lowSelector).Change().Select(x => -x);
        var updown = up.Zip(down, (x, y) => (Up: x, Down: y)).ToList();

        var atr = source.Atr(highSelector, lowSelector, closeSelector, diLength, AtrMethod.Rma).ToList();

        var plus = updown
            .Select(x =>
            {
                if (x.Up.HasValue && x.Down.HasValue)
                {
                    return x.Up.Value > x.Down.Value && x.Up.Value > 0 ? x.Up : 0;
                }
                return null;
            })
            .Rma(diLength)
            .Zip(atr, (x, y) => 100 * x / y)
            .FillNull()
            .ToList();

        var minus = updown
            .Select(x =>
            {
                if (x.Up.HasValue && x.Down.HasValue)
                {
                    return x.Down.Value > x.Up.Value && x.Down.Value > 0 ? x.Down : 0;
                }
                return null;
            })
            .Rma(diLength)
            .Zip(atr, (x, y) => 100 * x / y)
            .FillNull()
            .ToList();

        var absDiff = plus.Zip(minus, (p, m) => p - m).Abs();
        var safeSum = plus.Zip(minus, (p, m) => p + m).Select(x => x == 0 ? 1 : x);
        var adx = absDiff.Zip(safeSum, (d, s) => d / s).Rma(adxLength).Select(x => x * 100).ToList();

        return adx.Zip(plus, minus, (a, p, m) => new DMI(a, p, m));
    }

    public static IEnumerable<DMI> AverageDirectionalIndex(this IEnumerable<Kline> source, int adxLength = 14, int diLength = 14)
    {
        return source.AverageDirectionalIndex(x => x.HighPrice, x => x.LowPrice, x => x.ClosePrice, adxLength, diLength);
    }
}