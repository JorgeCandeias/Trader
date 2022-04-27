using Outcompute.Trader.Core.Mathematics;
using System.Collections;

namespace Outcompute.Trader.Indicators;

public abstract class IndicatorResult<TResult> : IIndicatorResult<TResult>
{
    public abstract TResult this[int index] { get; }

    public abstract int Count { get; }

    public abstract IEnumerator<TResult> GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

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