namespace Outcompute.Trader.Trading.Indicators;

/// <summary>
/// Provides easy access and discovery for composable indicators.
/// </summary>
public static class Indicator
{
    public static Abs Abs() => new();

    public static Abs Abs(IIndicatorResult<decimal?> source) => new(source);

    public static AbsLoss AbsLoss() => new();

    public static AbsLoss AbsLoss(IIndicatorResult<decimal?> source) => new(source);

    public static Add Add(IIndicatorResult<decimal?> first, IIndicatorResult<decimal?> second) => new(first, second);

    public static Change Change(int periods = 1) => new(periods);

    public static Change Change(IIndicatorResult<decimal?> source, int periods = 1) => new(source, periods);

    public static Divide Divide(IIndicatorResult<decimal?> first, IIndicatorResult<decimal?> second) => new(first, second);

    public static Multiply Multiply(IIndicatorResult<decimal?> first, IIndicatorResult<decimal?> second) => new(first, second);

    public static Subtract Subtract(IIndicatorResult<decimal?> first, IIndicatorResult<decimal?> second) => new(first, second);

    public static Zip<TFirstSource, TSecondSource, TResult> Zip<TFirstSource, TSecondSource, TResult>(IIndicatorResult<TFirstSource> first, IIndicatorResult<TSecondSource> second, Func<TFirstSource, TSecondSource, TResult> transform) => new(first, second, transform);

    public static Zip<decimal?, decimal?, decimal?> Zip(IIndicatorResult<decimal?> first, IIndicatorResult<decimal?> second, Func<decimal?, decimal?, decimal?> transform) => Zip<decimal?, decimal?, decimal?>(first, second, transform);

    public static Transform<TSource, TResult> Transform<TSource, TResult>(Func<TSource, TResult> transform) => new(transform);

    public static Transform<decimal?, decimal?> Transform(Func<decimal?, decimal?> transform) => Transform<decimal?, decimal?>(transform);

    public static Transform<TSource, TResult> Transform<TSource, TResult>(IIndicatorResult<TSource> source, Func<TSource, TResult> transform) => new(source, transform);

    public static Transform<decimal?, decimal?> Transform(IIndicatorResult<decimal?> source, Func<decimal?, decimal?> transform) => Transform<decimal?, decimal?>(source, transform);

    public static Sma Sma(int periods = Indicators.Sma.DefaultPeriods) => new(periods);

    public static Sma Sma(IIndicatorResult<decimal?> source, int periods = Indicators.Sma.DefaultPeriods) => new(source, periods);

    public static Rma Rma(int periods = Indicators.Rma.DefaultPeriods) => new(periods);

    public static Rma Rma(IIndicatorResult<decimal?> source, int periods = Indicators.Rma.DefaultPeriods) => new(source, periods);

    public static Ema Ema(int periods = Indicators.Ema.DefaultPeriods) => new(periods);

    public static Ema Ema(IIndicatorResult<decimal?> source, int periods = Indicators.Ema.DefaultPeriods) => new(source, periods);
}