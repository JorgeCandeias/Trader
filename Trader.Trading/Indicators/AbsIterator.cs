using Outcompute.Trader.Trading.Iterators;

namespace Outcompute.Trader.Trading.Indicators;

/// <summary>
/// An iterator that calculates the absolute value over the specified source.
/// </summary>
internal sealed class AbsIterator : Iterator<decimal>
{
    private readonly IEnumerable<decimal> _source;
    private readonly IEnumerator<decimal> _enumerator;

    public AbsIterator(IEnumerable<decimal> source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));

        _enumerator = _source.GetEnumerator();
        _state = 1;
    }

    public override bool MoveNext()
    {
        switch (_state)
        {
            case 1:
                if (_enumerator.MoveNext())
                {
                    _current = Math.Abs(_enumerator.Current);

                    return true;
                }
                else
                {
                    _state = -1;

                    return false;
                }
        }

        return false;
    }

    protected override Iterator<decimal> Clone() => new AbsIterator(_source);

    protected override void Dispose(bool disposing)
    {
        _state = -1;
        _enumerator.Dispose();

        base.Dispose(disposing);
    }
}