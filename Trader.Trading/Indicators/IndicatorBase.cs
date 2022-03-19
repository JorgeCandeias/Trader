using Outcompute.Trader.Core.Mathematics;
using Outcompute.Trader.Trading.Indicators.Operators;
using System.Collections;

namespace Outcompute.Trader.Trading.Indicators;

public abstract class IndicatorResult<TResult> : IIndicatorResult<TResult>
{
    public abstract TResult this[int index] { get; }

    public abstract int Count { get; }

    public abstract IEnumerator<TResult> GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #region Callbacks

    private readonly List<ChangeCallbackRegistration> _callbacks = new();

    private sealed class ChangeCallbackRegistration : IDisposable
    {
        private readonly IndicatorResult<TResult> _owner;
        private readonly Action<int> _action;

        public ChangeCallbackRegistration(IndicatorResult<TResult> owner, Action<int> action)
        {
            _owner = owner;
            _action = action;
        }

        public void RaiseCallback(int index)
        {
            _action(index);
        }

        private void DisposeCore()
        {
            _owner._callbacks.Remove(this);
        }

        public void Dispose()
        {
            DisposeCore();
            GC.SuppressFinalize(this);
        }

        ~ChangeCallbackRegistration()
        {
            DisposeCore();
        }
    }

    public IDisposable RegisterChangeCallback(Action<int> action)
    {
        var callback = new ChangeCallbackRegistration(this, action);

        _callbacks.Add(callback);

        return callback;
    }

    protected void RaiseCallback(int i)
    {
        for (var j = 0; j < _callbacks.Count; j++)
        {
            _callbacks[j].RaiseCallback(i);
        }
    }

    #endregion Callbacks

    #region Disposable

    private bool _disposed;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // noop
            }

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion Disposable

    #region Operators

    #region Add

    public static Add operator +(IndicatorResult<TResult> left, IndicatorResult<TResult> right)
    {
        if (left is IndicatorResult<decimal?> leftCast && right is IndicatorResult<decimal?> rightCast)
        {
            return new Add(leftCast, rightCast);
        }

        throw new NotSupportedException();
    }

    public static Transform<decimal?, decimal?> operator +(IndicatorResult<TResult> left, decimal? right)
    {
        if (left is IndicatorResult<decimal?> cast)
        {
            return new Transform<decimal?, decimal?>(cast, x => x + right);
        }

        throw new NotSupportedException();
    }

    public static Transform<decimal?, decimal?> operator +(decimal? first, IndicatorResult<TResult> right)
    {
        if (right is IndicatorResult<decimal?> cast)
        {
            return new Transform<decimal?, decimal?>(cast, x => x + first);
        }

        throw new NotSupportedException();
    }

    #endregion Add

    #region Subtract

    public static Subtract operator -(IndicatorResult<TResult> left, IndicatorResult<TResult> right)
    {
        if (left is IndicatorResult<decimal?> leftCast && right is IndicatorResult<decimal?> rightCast)
        {
            return new Subtract(leftCast, rightCast);
        }

        throw new NotSupportedException();
    }

    public static Transform<decimal?, decimal?> operator -(IndicatorResult<TResult> left, decimal? right)
    {
        if (left is IndicatorResult<decimal?> cast)
        {
            return new Transform<decimal?, decimal?>(cast, x => x - right);
        }

        throw new NotSupportedException();
    }

    public static Transform<decimal?, decimal?> operator -(decimal? left, IndicatorResult<TResult> right)
    {
        if (right is IndicatorResult<decimal?> cast)
        {
            return new Transform<decimal?, decimal?>(cast, x => left - x);
        }

        throw new NotSupportedException();
    }

    #endregion Subtract

    #region Multiply

    public static Multiply operator *(IndicatorResult<TResult> left, IndicatorResult<TResult> right)
    {
        if (left is IndicatorResult<decimal?> leftCast && right is IndicatorResult<decimal?> rightCast)
        {
            return new Multiply(leftCast, rightCast);
        }

        throw new NotSupportedException();
    }

    public static Transform<decimal?, decimal?> operator *(IndicatorResult<TResult> left, decimal? right)
    {
        if (left is IndicatorResult<decimal?> cast)
        {
            return new Transform<decimal?, decimal?>(cast, x => x * right);
        }

        throw new NotSupportedException();
    }

    public static Transform<decimal?, decimal?> operator *(decimal? left, IndicatorResult<TResult> right)
    {
        if (right is IndicatorResult<decimal?> cast)
        {
            return new Transform<decimal?, decimal?>(cast, x => left * x);
        }

        throw new NotSupportedException();
    }

    #endregion Multiply

    #region Divide

    public static Divide operator /(IndicatorResult<TResult> left, IndicatorResult<TResult> right)
    {
        if (left is IndicatorResult<decimal?> leftCast && right is IndicatorResult<decimal?> rightCast)
        {
            return new Divide(leftCast, rightCast);
        }

        throw new NotSupportedException();
    }

    public static Transform<decimal?, decimal?> operator /(IndicatorResult<TResult> left, decimal? right)
    {
        if (left is IndicatorResult<decimal?> cast)
        {
            return new Transform<decimal?, decimal?>(cast, x => MathN.SafeDiv(x, right));
        }

        throw new NotSupportedException();
    }

    public static IndicatorResult<decimal?> operator /(decimal? left, IndicatorResult<TResult> right)
    {
        if (right is IndicatorResult<decimal?> cast)
        {
            return new Transform<decimal?, decimal?>(cast, x => MathN.SafeDiv(left, x));
        }

        throw new NotSupportedException();
    }

    #endregion Divide

    #region Negative

    public static IndicatorResult<decimal?> operator -(IndicatorResult<TResult> self)
    {
        if (self is IndicatorResult<decimal?> cast)
        {
            return new Transform<decimal?, decimal?>(cast, x => -x);
        }

        throw new NotSupportedException();
    }

    #endregion Negative

    #endregion Operators
}

public class IndicatorRootBase<TResult> : IndicatorResult<TResult>
{
    private readonly List<TResult> _result = new();

    public override TResult this[int index] => _result[index];

    public override int Count => _result.Count;

    protected void Set(int index, TResult value)
    {
        Guard.IsGreaterThanOrEqualTo(index, 0, nameof(index));

        // if the value is in range then update it
        if (index < _result.Count)
        {
            _result[index] = value;
            RaiseCallback(index);
            return;
        }

        // add all gaps up to the new value
        for (var i = _result.Count; i < index; i++)
        {
            _result.Add(default!);
            RaiseCallback(index);
        }

        // add the new value at the end
        _result.Add(value);
        RaiseCallback(index);
    }

    public override IEnumerator<TResult> GetEnumerator() => _result.GetEnumerator();
}

public sealed class Identity<TResult> : IndicatorRootBase<TResult>, IIndicatorSource<TResult>
{
    public void Add(TResult value)
    {
        Set(Count, value);
    }

    public void Update(int index, TResult value)
    {
        Set(index, value);
    }
}

public static partial class Indicator
{
    public static Identity<T> Identity<T>() => new();

    public static Identity<T> Identity<T>(params T[] array)
    {
        var identity = new Identity<T>();

        foreach (var item in array)
        {
            identity.Add(item);
        }

        return identity;
    }

    public static Identity<T> Identity<T>(this IEnumerable<T> source)
    {
        var identity = new Identity<T>();

        foreach (var item in source)
        {
            identity.Add(item);
        }

        return identity;
    }
}

public class CompositeIndicator<TSource, TResult> : IndicatorResult<TResult>
{
    private readonly IndicatorResult<TResult> _result;

    protected CompositeIndicator(IndicatorResult<TSource> source, Func<IndicatorResult<TSource>, IndicatorResult<TResult>> compose)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(compose, nameof(compose));

        _result = compose(source);
    }

    public override TResult this[int index] => _result[index];

    public override int Count => _result.Count;

    public override IEnumerator<TResult> GetEnumerator() => _result.GetEnumerator();
}

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