using System;
using System.Collections.Generic;

namespace Outcompute.Trader.Trading.Indicators
{
    /// <summary>
    /// An iterator that calculates the RMA over the specified source.
    /// </summary>
    internal sealed class RmaIterator : Iterator<decimal>
    {
        private readonly IEnumerable<decimal> _source;
        private readonly IEnumerator<decimal> _enumerator;
        private readonly int _periods;

        public RmaIterator(IEnumerable<decimal> source, int periods)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _periods = periods > 0 ? periods : throw new ArgumentOutOfRangeException(nameof(periods));

            _enumerator = source.GetEnumerator();
            _state = 1;
        }

        public override bool MoveNext()
        {
            switch (_state)
            {
                // rma - first value
                case 1:
                    if (_enumerator.MoveNext())
                    {
                        _current = _enumerator.Current;
                        _state = 2;

                        return true;
                    }
                    else
                    {
                        _state = -1;

                        return false;
                    }

                // rma - following values
                case 2:
                    if (_enumerator.MoveNext())
                    {
                        var value = _enumerator.Current;

                        _current = (((_periods - 1) * _current) + value) / _periods;

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

        protected override Iterator<decimal> Clone() => new RmaIterator(_source, _periods);

        protected override void Dispose(bool disposing)
        {
            _state = -1;
            _enumerator.Dispose();

            base.Dispose(disposing);
        }
    }
}