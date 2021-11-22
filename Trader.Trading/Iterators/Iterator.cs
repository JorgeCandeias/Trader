using System.Collections;

namespace Outcompute.Trader.Trading.Iterators;

/// <summary>
/// Base class for high-performance self-enumerators.
/// </summary>
internal abstract class Iterator<TSource> : IEnumerable<TSource>, IEnumerator<TSource>
{
    private bool _used;
    protected int _state;
    protected TSource _current = default!;

    protected Iterator()
    {
    }

    public TSource Current
    {
        get
        {
            if (_state < 1)
            {
                throw new InvalidOperationException();
            }

            return _current;
        }
    }

    protected abstract Iterator<TSource> Clone();

    public IEnumerator<TSource> GetEnumerator()
    {
        if (_used)
        {
            return Clone();
        }
        else
        {
            _used = true;
            return this;
        }
    }

    public abstract bool MoveNext();

    protected virtual void Dispose(bool disposing)
    {
        _current = default!;
        _state = -1;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    object? IEnumerator.Current => Current;

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    void IEnumerator.Reset() => throw new NotSupportedException();
}