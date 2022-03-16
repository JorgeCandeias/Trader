using Outcompute.Trader.Core.Mathematics;
using System.Collections;

namespace Outcompute.Trader.Trading.Indicators;

public abstract class IndicatorBase<TSource, TResult> : IIndicator<TSource, TResult>
{
    protected List<TSource> Source { get; } = new List<TSource>();

    protected List<TResult> Result { get; } = new List<TResult>();

    public TResult this[int index] => Result[index];

    public int Count => Result.Count;

    public void Add(TSource value)
    {
        Update(Source.Count, value);
    }

    public void Update(int index, TSource value)
    {
        Guard.IsGreaterThanOrEqualTo(index, 0, nameof(index));

        // allocate up to and including the target index
        var count = Source.Count;
        for (var i = count; i <= index; i++)
        {
            Source.Add(default!);
            Result.Add(default!);
        }

        // assign the source value
        Source[index] = value;

        // calculate forward since the first affected index
        for (var i = Math.Min(count, index); i < Source.Count; i++)
        {
            Result[i] = Calculate(i);

            // notify downstream
            for (var j = 0; j < _registrations.Count; j++)
            {
                _registrations[j].RaiseCallback(i, Result[i]);
            }
        }
    }

    protected abstract TResult Calculate(int index);

    public IEnumerator<TResult> GetEnumerator() => Result.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => Result.GetEnumerator();

    #region Callbacks

    /// <summary>
    /// Links that a downstream target maintains to an upstream source.
    /// </summary>
    private readonly List<IDisposable> _links = new();

    /// <summary>
    /// Links that an upstream source maintains about a dowstream target.
    /// </summary>
    private readonly List<ChangeCallbackRegistration> _registrations = new();

    /// <summary>
    /// Links this downstream target to an upstream source.
    /// </summary>
    protected void LinkFrom(IIndicatorResult<TSource> source)
    {
        Guard.IsNotNull(source, nameof(source));

        for (var i = 0; i < source.Count; i++)
        {
            Update(i, source[i]);
        }

        _links.Add(source.RegisterChangeCallback(Update));
    }

    /// <summary>
    /// Links this upstream to a dowstream target.
    /// </summary>
    public IDisposable RegisterChangeCallback(Action<int, TResult> action)
    {
        Guard.IsNotNull(action, nameof(action));

        var registration = new ChangeCallbackRegistration(this, action);

        _registrations.Add(registration);

        return registration;
    }

    private sealed class ChangeCallbackRegistration : IDisposable
    {
        private readonly IndicatorBase<TSource, TResult> _owner;
        private readonly Action<int, TResult> _action;

        public ChangeCallbackRegistration(IndicatorBase<TSource, TResult> owner, Action<int, TResult> action)
        {
            _owner = owner;
            _action = action;
        }

        public void RaiseCallback(int index, TResult value)
        {
            _action(index, value);
        }

        private void DisposeCore()
        {
            _owner._registrations.Remove(this);
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

    #endregion Callbacks

    #region Disposable

    protected virtual void Dispose(bool disposing)
    {
        foreach (var link in _links)
        {
            link.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~IndicatorBase()
    {
        Dispose(false);
    }

    #endregion Disposable

    #region Operators

    #region Add

    public static Add operator +(IndicatorBase<TSource, TResult> first, IIndicatorResult<TResult> second)
    {
        if (typeof(TResult) == typeof(decimal?))
        {
            return new Add((IIndicatorResult<decimal?>)first, (IIndicatorResult<decimal?>)second);
        }

        throw new NotSupportedException();
    }

    public static Add operator +(IIndicatorResult<TResult> first, IndicatorBase<TSource, TResult> second)
    {
        if (typeof(TResult) == typeof(decimal?))
        {
            return new Add((IIndicatorResult<decimal?>)first, (IIndicatorResult<decimal?>)second);
        }

        throw new NotSupportedException();
    }

    public static Transform<decimal?, decimal?> operator +(IndicatorBase<TSource, TResult> first, decimal? second)
    {
        if (typeof(TResult) == typeof(decimal?))
        {
            return new Transform<decimal?, decimal?>((IIndicatorResult<decimal?>)first, x => x + second);
        }

        throw new NotSupportedException();
    }

    public static Transform<decimal?, decimal?> operator +(decimal? first, IndicatorBase<TSource, TResult> second)
    {
        if (typeof(TResult) == typeof(decimal?))
        {
            return new Transform<decimal?, decimal?>((IIndicatorResult<decimal?>)second, x => x + first);
        }

        throw new NotSupportedException();
    }

    #endregion Add

    #region Subtract

    public static Subtract operator -(IndicatorBase<TSource, TResult> first, IIndicatorResult<TResult> second)
    {
        if (typeof(TResult) == typeof(decimal?))
        {
            return new Subtract((IIndicatorResult<decimal?>)first, (IIndicatorResult<decimal?>)second);
        }

        throw new NotSupportedException();
    }

    public static Transform<decimal?, decimal?> operator -(IndicatorBase<TSource, TResult> first, decimal? second)
    {
        if (typeof(TResult) == typeof(decimal?))
        {
            return new Transform<decimal?, decimal?>((IIndicatorResult<decimal?>)first, x => x - second);
        }

        throw new NotSupportedException();
    }

    public static Transform<decimal?, decimal?> operator -(decimal? first, IndicatorBase<TSource, TResult> second)
    {
        if (typeof(TResult) == typeof(decimal?))
        {
            return new Transform<decimal?, decimal?>((IIndicatorResult<decimal?>)second, x => first - x);
        }

        throw new NotSupportedException();
    }

    #endregion Subtract

    #region Multiply

    public static Multiply operator *(IndicatorBase<TSource, TResult> first, IIndicatorResult<TResult> second)
    {
        if (typeof(TResult) == typeof(decimal?))
        {
            return new Multiply((IIndicatorResult<decimal?>)first, (IIndicatorResult<decimal?>)second);
        }

        throw new NotSupportedException();
    }

    public static Transform<decimal?, decimal?> operator *(IndicatorBase<TSource, TResult> first, decimal? second)
    {
        if (typeof(TResult) == typeof(decimal?))
        {
            return new Transform<decimal?, decimal?>((IIndicatorResult<decimal?>)first, x => x * second);
        }

        throw new NotSupportedException();
    }

    public static Transform<decimal?, decimal?> operator *(decimal? first, IndicatorBase<TSource, TResult> second)
    {
        if (typeof(TResult) == typeof(decimal?))
        {
            return new Transform<decimal?, decimal?>((IIndicatorResult<decimal?>)second, x => first * x);
        }

        throw new NotSupportedException();
    }

    #endregion Multiply

    #region Divide

    public static Divide operator /(IndicatorBase<TSource, TResult> first, IIndicatorResult<TResult> second)
    {
        if (typeof(TResult) == typeof(decimal?))
        {
            return new Divide((IIndicatorResult<decimal?>)first, (IIndicatorResult<decimal?>)second);
        }

        throw new NotSupportedException();
    }

    public static Divide operator /(IIndicatorResult<TResult> first, IndicatorBase<TSource, TResult> second)
    {
        if (typeof(TResult) == typeof(decimal?))
        {
            return new Divide((IIndicatorResult<decimal?>)first, (IIndicatorResult<decimal?>)second);
        }

        throw new NotSupportedException();
    }

    public static Transform<decimal?, decimal?> operator /(IndicatorBase<TSource, TResult> first, decimal? second)
    {
        if (typeof(TResult) == typeof(decimal?))
        {
            return new Transform<decimal?, decimal?>((IIndicatorResult<decimal?>)first, x => MathN.SafeDiv(x, second));
        }

        throw new NotSupportedException();
    }

    public static Transform<decimal?, decimal?> operator /(decimal? first, IndicatorBase<TSource, TResult> second)
    {
        if (typeof(TResult) == typeof(decimal?))
        {
            return new Transform<decimal?, decimal?>((IIndicatorResult<decimal?>)second, x => MathN.SafeDiv(first, x));
        }

        throw new NotSupportedException();
    }

    #endregion Divide

    #endregion Operators
}