namespace Outcompute.Trader.Trading.Indicators;

/// <summary>
/// Indicator that zips two source indicators using the specified transform function.
/// </summary>
/// <remarks>
/// This indicator does not support in-place updates.
/// </remarks>
public class Zip<TFirstSource, TSecondSource, TResult> : IndicatorBase<TResult, TResult>
{
    private readonly Func<TFirstSource, TSecondSource, TResult> _transform;
    private readonly IIndicatorResult<TFirstSource> _first;
    private readonly IIndicatorResult<TSecondSource> _second;
    private readonly IDisposable _firstCallback;
    private readonly IDisposable _secondCallback;

    public Zip(IIndicatorResult<TFirstSource> first, IIndicatorResult<TSecondSource> second, Func<TFirstSource, TSecondSource, TResult> transform)
    {
        Guard.IsNotNull(first, nameof(first));
        Guard.IsNotNull(second, nameof(second));
        Guard.IsNotNull(transform, nameof(transform));

        _transform = transform;
        _first = first;
        _second = second;

        var count = Math.Max(first.Count, second.Count);
        for (var i = 0; i < count; i++)
        {
            UpdateCore(i, default!);
        }

        _firstCallback = _first.RegisterChangeCallback((index, _) => UpdateCore(index, default!));
        _secondCallback = _second.RegisterChangeCallback((index, _) => UpdateCore(index, default!));
    }

    public override void Add(TResult value)
    {
        ThrowHelper.ThrowNotSupportedException();
    }

    public override void Update(int index, TResult value)
    {
        ThrowHelper.ThrowNotSupportedException();
    }

    protected override TResult Calculate(int index)
    {
        var first = index < _first.Count ? _first[index] : default!;
        var second = index < _second.Count ? _second[index] : default!;

        return _transform(first, second);
    }

    protected override void Dispose(bool disposing)
    {
        _firstCallback.Dispose();
        _secondCallback.Dispose();

        base.Dispose(disposing);
    }
}

/// <summary>
/// Indicator that zips two source indicators using the specified transform function.
/// </summary>
/// <remarks>
/// This indicator does not support in-place updates.
/// </remarks>
public class Zip<TFirst, TSecond, TThird, TResult> : IndicatorBase<TResult, TResult>
{
    private readonly Func<TFirst, TSecond, TThird, TResult> _transform;
    private readonly IIndicatorResult<TFirst> _first;
    private readonly IIndicatorResult<TSecond> _second;
    private readonly IIndicatorResult<TThird> _third;
    private readonly IDisposable _firstCallback;
    private readonly IDisposable _secondCallback;
    private readonly IDisposable _thirdCallback;

    public Zip(IIndicatorResult<TFirst> first, IIndicatorResult<TSecond> second, IIndicatorResult<TThird> third, Func<TFirst, TSecond, TThird, TResult> transform)
    {
        Guard.IsNotNull(first, nameof(first));
        Guard.IsNotNull(second, nameof(second));
        Guard.IsNotNull(third, nameof(third));
        Guard.IsNotNull(transform, nameof(transform));

        _transform = transform;
        _first = first;
        _second = second;
        _third = third;

        var count = Math.Max(first.Count, second.Count);
        for (var i = 0; i < count; i++)
        {
            UpdateCore(i, default!);
        }

        _firstCallback = _first.RegisterChangeCallback((index, _) => UpdateCore(index, default!));
        _secondCallback = _second.RegisterChangeCallback((index, _) => UpdateCore(index, default!));
        _thirdCallback = _third.RegisterChangeCallback((index, _) => UpdateCore(index, default!));
    }

    public override void Add(TResult value)
    {
        ThrowHelper.ThrowNotSupportedException();
    }

    public override void Update(int index, TResult value)
    {
        ThrowHelper.ThrowNotSupportedException();
    }

    protected override TResult Calculate(int index)
    {
        var first = index < _first.Count ? _first[index] : default!;
        var second = index < _second.Count ? _second[index] : default!;
        var third = index < _third.Count ? _third[index] : default!;

        return _transform(first, second, third);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _firstCallback.Dispose();
            _secondCallback.Dispose();
            _thirdCallback.Dispose();
        }

        base.Dispose(disposing);
    }
}

public static partial class Indicator
{
    public static Zip<TFirstSource, TSecondSource, TResult> Zip<TFirstSource, TSecondSource, TResult>(IIndicatorResult<TFirstSource> first, IIndicatorResult<TSecondSource> second, Func<TFirstSource, TSecondSource, TResult> transform) => new(first, second, transform);

    public static Zip<decimal?, decimal?, decimal?> Zip(IIndicatorResult<decimal?> first, IIndicatorResult<decimal?> second, Func<decimal?, decimal?, decimal?> transform) => Zip<decimal?, decimal?, decimal?>(first, second, transform);

    public static Zip<TFirst, TSecond, TThird, TResult> Zip<TFirst, TSecond, TThird, TResult>(IIndicatorResult<TFirst> first, IIndicatorResult<TSecond> second, IIndicatorResult<TThird> third, Func<TFirst, TSecond, TThird, TResult> transform) => new(first, second, third, transform);

    public static Zip<decimal?, decimal?, decimal?, decimal?> Zip(IIndicatorResult<decimal?> first, IIndicatorResult<decimal?> second, IIndicatorResult<decimal?> third, Func<decimal?, decimal?, decimal?, decimal?> transform) => Zip<decimal?, decimal?, decimal?, decimal?>(first, second, third, transform);
}