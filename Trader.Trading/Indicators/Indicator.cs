﻿namespace Outcompute.Trader.Trading.Indicators;

/// <summary>
/// Provides easy access and discovery for composable indicators.
/// </summary>
public static class Indicator
{
    public static Identity<T> Identity<T>() => new();

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

    public static Zip<TFirst, TSecond, TThird, TResult> Zip<TFirst, TSecond, TThird, TResult>(IIndicatorResult<TFirst> first, IIndicatorResult<TSecond> second, IIndicatorResult<TThird> third, Func<TFirst, TSecond, TThird, TResult> transform) => new(first, second, third, transform);

    public static Zip<decimal?, decimal?, decimal?, decimal?> Zip(IIndicatorResult<decimal?> first, IIndicatorResult<decimal?> second, IIndicatorResult<decimal?> third, Func<decimal?, decimal?, decimal?, decimal?> transform) => Zip<decimal?, decimal?, decimal?, decimal?>(first, second, third, transform);

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

    public static TrueRange TrueRange() => new();

    public static TrueRange TrueRange(IIndicatorResult<HLC> source) => new(source);

    public static Wma Wma(int periods = Indicators.Wma.DefaultPeriods) => new(periods);

    public static Wma Wma(IIndicatorResult<decimal?> source, int periods = Indicators.Wma.DefaultPeriods) => new(source, periods);

    public static Hma Hma(int periods = Indicators.Hma.DefaultPeriods) => new(periods);

    public static Hma Hma(IIndicatorResult<decimal?> source, int periods = Indicators.Hma.DefaultPeriods) => new(source, periods);

    public static Atr Atr(int periods = Indicators.Atr.DefaultPeriods, AtrMethod method = Indicators.Atr.DefaultAtrMethod) => new(periods, method);

    public static Atr Atr(IIndicatorResult<HLC> source, int periods = Indicators.Atr.DefaultPeriods, AtrMethod method = Indicators.Atr.DefaultAtrMethod) => new(source, periods, method);

    public static HL2 HL2() => new();

    public static HL2 HL2(IIndicatorResult<HL> source) => new(source);

    public static AwesomeOscillator AwesomeOscillator(int fastPeriods = Indicators.AwesomeOscillator.DefaultFastPeriods, int slowPeriods = Indicators.AwesomeOscillator.DefaultSlowPeriods) => new(fastPeriods, slowPeriods);

    public static AwesomeOscillator AwesomeOscillator(IIndicatorResult<HL> source, int fastPeriods = Indicators.AwesomeOscillator.DefaultFastPeriods, int slowPeriods = Indicators.AwesomeOscillator.DefaultSlowPeriods) => new(source, fastPeriods, slowPeriods);

    public static Variance Variance(int periods = Indicators.Variance.DefaultPeriods) => new(periods);

    public static Variance Variance(IIndicatorResult<decimal?> source, int periods = Indicators.Variance.DefaultPeriods) => new(source, periods);

    public static FillNull<T> FillNull<T>() => new();

    public static FillNull<T> FillNull<T>(IIndicatorResult<T> source) => new(source);
}