namespace Outcompute.Trader.Trading.Indicators;

/// <summary>
/// Indicator that zips two source indicators using the specified transform function.
/// </summary>
public class Zip<TFirstSource, TSecondSource, TResult> : IndicatorRootBase<TResult>
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

        _first = first;
        _second = second;
        _transform = transform;

        var count = Math.Max(first.Count, second.Count);
        for (var i = 0; i < count; i++)
        {
            Update(i);
        }

        _firstCallback = _first.RegisterChangeCallback(Update);
        _secondCallback = _second.RegisterChangeCallback(Update);
    }

    private void Update(int index)
    {
        var first = index < _first.Count ? _first[index] : default!;
        var second = index < _second.Count ? _second[index] : default!;
        var result = _transform(first, second);

        Set(index, result);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _firstCallback.Dispose();
            _secondCallback.Dispose();
        }

        base.Dispose(disposing);
    }
}

/// <summary>
/// Indicator that zips three source indicators using the specified transform function.
/// </summary>
/// <remarks>
/// This indicator does not support in-place updates.
/// </remarks>
public class Zip<TFirst, TSecond, TThird, TResult> : IndicatorRootBase<TResult>
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

        _first = first;
        _second = second;
        _third = third;
        _transform = transform;

        var count = Math.Max(Math.Max(first.Count, second.Count), third.Count);
        for (var i = 0; i < count; i++)
        {
            Update(i);
        }

        _firstCallback = _first.RegisterChangeCallback(Update);
        _secondCallback = _second.RegisterChangeCallback(Update);
        _thirdCallback = _third.RegisterChangeCallback(Update);
    }

    private void Update(int index)
    {
        var first = index < _first.Count ? _first[index] : default!;
        var second = index < _second.Count ? _second[index] : default!;
        var third = index < _third.Count ? _third[index] : default!;
        var result = _transform(first, second, third);

        Set(index, result);
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

/// <summary>
/// Indicator that zips four source indicators using the specified transform function.
/// </summary>
/// <remarks>
/// This indicator does not support in-place updates.
/// </remarks>
public class Zip<TFirst, TSecond, TThird, TFourth, TResult> : IndicatorRootBase<TResult>
{
    private readonly Func<TFirst, TSecond, TThird, TFourth, TResult> _transform;
    private readonly IIndicatorResult<TFirst> _first;
    private readonly IIndicatorResult<TSecond> _second;
    private readonly IIndicatorResult<TThird> _third;
    private readonly IIndicatorResult<TFourth> _fourth;
    private readonly IDisposable _firstCallback;
    private readonly IDisposable _secondCallback;
    private readonly IDisposable _thirdCallback;
    private readonly IDisposable _fourthCallback;

    public Zip(IIndicatorResult<TFirst> first, IIndicatorResult<TSecond> second, IIndicatorResult<TThird> third, IIndicatorResult<TFourth> fourth, Func<TFirst, TSecond, TThird, TFourth, TResult> transform)
    {
        Guard.IsNotNull(first, nameof(first));
        Guard.IsNotNull(second, nameof(second));
        Guard.IsNotNull(third, nameof(third));
        Guard.IsNotNull(fourth, nameof(fourth));
        Guard.IsNotNull(transform, nameof(transform));

        _first = first;
        _second = second;
        _third = third;
        _fourth = fourth;
        _transform = transform;

        var count = Math.Max(Math.Max(Math.Max(first.Count, second.Count), third.Count), fourth.Count);
        for (var i = 0; i < count; i++)
        {
            Update(i);
        }

        _firstCallback = _first.RegisterChangeCallback(Update);
        _secondCallback = _second.RegisterChangeCallback(Update);
        _thirdCallback = _third.RegisterChangeCallback(Update);
        _fourthCallback = _fourth.RegisterChangeCallback(Update);
    }

    private void Update(int index)
    {
        var first = index < _first.Count ? _first[index] : default!;
        var second = index < _second.Count ? _second[index] : default!;
        var third = index < _third.Count ? _third[index] : default!;
        var fourth = index < _fourth.Count ? _fourth[index] : default!;
        var result = _transform(first, second, third, fourth);

        Set(index, result);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _firstCallback.Dispose();
            _secondCallback.Dispose();
            _thirdCallback.Dispose();
            _fourthCallback.Dispose();
        }

        base.Dispose(disposing);
    }
}

/// <summary>
/// Indicator that zips four source indicators using the specified transform function.
/// </summary>
/// <remarks>
/// This indicator does not support in-place updates.
/// </remarks>
public class Zip<TFirst, TSecond, TThird, TFourth, TFifth, TResult> : IndicatorRootBase<TResult>
{
    private readonly Func<TFirst, TSecond, TThird, TFourth, TFifth, TResult> _transform;
    private readonly IIndicatorResult<TFirst> _first;
    private readonly IIndicatorResult<TSecond> _second;
    private readonly IIndicatorResult<TThird> _third;
    private readonly IIndicatorResult<TFourth> _fourth;
    private readonly IIndicatorResult<TFifth> _fifth;
    private readonly IDisposable _firstCallback;
    private readonly IDisposable _secondCallback;
    private readonly IDisposable _thirdCallback;
    private readonly IDisposable _fourthCallback;
    private readonly IDisposable _fifthCallback;

    public Zip(IIndicatorResult<TFirst> first, IIndicatorResult<TSecond> second, IIndicatorResult<TThird> third, IIndicatorResult<TFourth> fourth, IIndicatorResult<TFifth> fifth, Func<TFirst, TSecond, TThird, TFourth, TFifth, TResult> transform)
    {
        Guard.IsNotNull(first, nameof(first));
        Guard.IsNotNull(second, nameof(second));
        Guard.IsNotNull(third, nameof(third));
        Guard.IsNotNull(fourth, nameof(fourth));
        Guard.IsNotNull(fifth, nameof(fifth));
        Guard.IsNotNull(transform, nameof(transform));

        _first = first;
        _second = second;
        _third = third;
        _fourth = fourth;
        _fifth = fifth;
        _transform = transform;

        var count = Math.Max(Math.Max(Math.Max(Math.Max(first.Count, second.Count), third.Count), fourth.Count), fifth.Count);
        for (var i = 0; i < count; i++)
        {
            Update(i);
        }

        _firstCallback = _first.RegisterChangeCallback(Update);
        _secondCallback = _second.RegisterChangeCallback(Update);
        _thirdCallback = _third.RegisterChangeCallback(Update);
        _fourthCallback = _fourth.RegisterChangeCallback(Update);
        _fifthCallback = _fifth.RegisterChangeCallback(Update);
    }

    private void Update(int index)
    {
        var first = index < _first.Count ? _first[index] : default!;
        var second = index < _second.Count ? _second[index] : default!;
        var third = index < _third.Count ? _third[index] : default!;
        var fourth = index < _fourth.Count ? _fourth[index] : default!;
        var fifth = index < _fifth.Count ? _fifth[index] : default!;
        var result = _transform(first, second, third, fourth, fifth);

        Set(index, result);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _firstCallback.Dispose();
            _secondCallback.Dispose();
            _thirdCallback.Dispose();
            _fourthCallback.Dispose();
            _fifthCallback.Dispose();
        }

        base.Dispose(disposing);
    }
}

/// <summary>
/// Indicator that zips four source indicators using the specified transform function.
/// </summary>
/// <remarks>
/// This indicator does not support in-place updates.
/// </remarks>
public class Zip<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TResult> : IndicatorRootBase<TResult>
{
    private readonly Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TResult> _transform;
    private readonly IIndicatorResult<TFirst> _first;
    private readonly IIndicatorResult<TSecond> _second;
    private readonly IIndicatorResult<TThird> _third;
    private readonly IIndicatorResult<TFourth> _fourth;
    private readonly IIndicatorResult<TFifth> _fifth;
    private readonly IIndicatorResult<TSixth> _sixth;
    private readonly IDisposable _firstCallback;
    private readonly IDisposable _secondCallback;
    private readonly IDisposable _thirdCallback;
    private readonly IDisposable _fourthCallback;
    private readonly IDisposable _fifthCallback;
    private readonly IDisposable _sixthCallback;

    public Zip(IIndicatorResult<TFirst> first, IIndicatorResult<TSecond> second, IIndicatorResult<TThird> third, IIndicatorResult<TFourth> fourth, IIndicatorResult<TFifth> fifth, IIndicatorResult<TSixth> sixth, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TResult> transform)
    {
        Guard.IsNotNull(first, nameof(first));
        Guard.IsNotNull(second, nameof(second));
        Guard.IsNotNull(third, nameof(third));
        Guard.IsNotNull(fourth, nameof(fourth));
        Guard.IsNotNull(fifth, nameof(fifth));
        Guard.IsNotNull(sixth, nameof(sixth));
        Guard.IsNotNull(transform, nameof(transform));

        _first = first;
        _second = second;
        _third = third;
        _fourth = fourth;
        _fifth = fifth;
        _sixth = sixth;
        _transform = transform;

        var count = Math.Max(Math.Max(Math.Max(Math.Max(Math.Max(first.Count, second.Count), third.Count), fourth.Count), fifth.Count), sixth.Count);
        for (var i = 0; i < count; i++)
        {
            Update(i);
        }

        _firstCallback = _first.RegisterChangeCallback(Update);
        _secondCallback = _second.RegisterChangeCallback(Update);
        _thirdCallback = _third.RegisterChangeCallback(Update);
        _fourthCallback = _fourth.RegisterChangeCallback(Update);
        _fifthCallback = _fifth.RegisterChangeCallback(Update);
        _sixthCallback = _sixth.RegisterChangeCallback(Update);
    }

    private void Update(int index)
    {
        var first = index < _first.Count ? _first[index] : default!;
        var second = index < _second.Count ? _second[index] : default!;
        var third = index < _third.Count ? _third[index] : default!;
        var fourth = index < _fourth.Count ? _fourth[index] : default!;
        var fifth = index < _fifth.Count ? _fifth[index] : default!;
        var sixth = index < _sixth.Count ? _sixth[index] : default!;
        var result = _transform(first, second, third, fourth, fifth, sixth);

        Set(index, result);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _firstCallback.Dispose();
            _secondCallback.Dispose();
            _thirdCallback.Dispose();
            _fourthCallback.Dispose();
            _fifthCallback.Dispose();
            _sixthCallback.Dispose();
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

    public static Zip<TFirst, TSecond, TThird, TFourth, TResult> Zip<TFirst, TSecond, TThird, TFourth, TResult>(IIndicatorResult<TFirst> first, IIndicatorResult<TSecond> second, IIndicatorResult<TThird> third, IIndicatorResult<TFourth> fourth, Func<TFirst, TSecond, TThird, TFourth, TResult> transform) => new(first, second, third, fourth, transform);

    public static Zip<decimal?, decimal?, decimal?, decimal?, decimal?> Zip(IIndicatorResult<decimal?> first, IIndicatorResult<decimal?> second, IIndicatorResult<decimal?> third, IIndicatorResult<decimal?> fourth, Func<decimal?, decimal?, decimal?, decimal?, decimal?> transform) => Zip<decimal?, decimal?, decimal?, decimal?, decimal?>(first, second, third, fourth, transform);

    public static Zip<TFirst, TSecond, TThird, TFourth, TFifth, TResult> Zip<TFirst, TSecond, TThird, TFourth, TFifth, TResult>(IIndicatorResult<TFirst> first, IIndicatorResult<TSecond> second, IIndicatorResult<TThird> third, IIndicatorResult<TFourth> fourth, IIndicatorResult<TFifth> fifth, Func<TFirst, TSecond, TThird, TFourth, TFifth, TResult> transform) => new(first, second, third, fourth, fifth, transform);

    public static Zip<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TResult> Zip<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TResult>(IIndicatorResult<TFirst> first, IIndicatorResult<TSecond> second, IIndicatorResult<TThird> third, IIndicatorResult<TFourth> fourth, IIndicatorResult<TFifth> fifth, IIndicatorResult<TSixth> sixth, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TResult> transform)
        => new(first, second, third, fourth, fifth, sixth, transform);
}