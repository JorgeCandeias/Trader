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