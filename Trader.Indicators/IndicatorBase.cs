namespace Outcompute.Trader.Indicators;

public abstract class IndicatorBase<TSource, TResult> : IndicatorResult<TResult>
{
    private readonly Identity<TResult> _result = new();
    private readonly bool _updateForward;
    private bool _ready;

    /// <summary>
    /// Attaches this series to a source series and configures behaviour.
    /// </summary>
    /// <param name="source">The source to attach this series to.</param>
    /// <param name="updateForward">
    /// Whether to update the series forward on every source index change.
    /// Use true for series where the later values depend on earlier values, such as moving averages.
    /// Use false for series where each value is independant from others, to improve performance.
    /// </param>
    protected IndicatorBase(IndicatorResult<TSource> source, bool updateForward)
    {
        Guard.IsNotNull(source, nameof(source));

        Source = source;

        _updateForward = updateForward;
    }

    protected IndicatorResult<TSource> Source { get; }

    protected IndicatorResult<TResult> Result
    {
        get
        {
            EnsureReady();
            return _result;
        }
    }

    public override TResult this[int index]
    {
        get
        {
            EnsureReady();
            return Result[index];
        }
    }

    public override IEnumerator<TResult> GetEnumerator()
    {
        EnsureReady();
        return Result.GetEnumerator();
    }

    public override int Count
    {
        get
        {
            EnsureReady();
            return Result.Count;
        }
    }

    private void Consume(int index)
    {
        // update the target value
        ConsumeCore(index);

        // if this series must update forward then update all following values too
        if (_updateForward)
        {
            for (var i = index + 1; i < Count; i++)
            {
                ConsumeCore(i);
            }
        }
    }

    private void ConsumeCore(int index)
    {
        _result.Update(index, default!);
        _result.Update(index, Calculate(index));
    }

    protected abstract TResult Calculate(int index);

    protected void Ready()
    {
        _ready = true;

        for (var i = 0; i < Source.Count; i++)
        {
            ConsumeCore(i);
        }

        _links.Add(Source.RegisterChangeCallback(Consume));
    }

    private void EnsureReady()
    {
        if (!_ready)
        {
            ThrowHelper.ThrowInvalidOperationException(
                $"{nameof(IndicatorBase<TSource, TResult>)} is not ready. Call {nameof(Ready)}() in the derived class constructor after any derived logic is configured.");
        }
    }

    #region Result Callbacks

    /// <summary>
    /// Links that a downstream target maintains to an upstream source.
    /// </summary>
    private readonly List<IDisposable> _links = new();

    #endregion Result Callbacks

    #region Disposable

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var link in _links)
            {
                link.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    #endregion Disposable
}