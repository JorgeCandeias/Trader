﻿using Outcompute.Trader.Trading.Iterators;

namespace Outcompute.Trader.Trading.Indicators;

/// <summary>
/// An iterator that calculates the change between the current value and the previous value over the specified source.
/// </summary>
internal sealed class ChangeIterator : Iterator<decimal>
{
    private readonly IEnumerable<decimal> _source;
    private readonly IEnumerator<decimal> _enumerator;

    private decimal _last;

    public ChangeIterator(IEnumerable<decimal> source)
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
                    _last = _enumerator.Current;
                    _current = 0m;
                    _state = 2;

                    return true;
                }
                else
                {
                    _state = -1;

                    return false;
                }

            case 2:
                if (_enumerator.MoveNext())
                {
                    var value = _enumerator.Current;
                    _current = value - _last;
                    _last = value;

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

    protected override Iterator<decimal> Clone() => new ChangeIterator(_source);

    protected override void Dispose(bool disposing)
    {
        _state = -1;
        _enumerator.Dispose();

        base.Dispose(disposing);
    }
}