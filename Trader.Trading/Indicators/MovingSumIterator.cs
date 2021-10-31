using Outcompute.Trader.Core.Pooling;
using System;
using System.Collections.Generic;

namespace Outcompute.Trader.Trading.Indicators
{
    /// <summary>
    /// An iterator that calculates the SMA over the specified source.
    /// </summary>
    internal sealed class MovingSumIterator : Iterator<decimal>
    {
        private readonly IEnumerable<decimal> _source;
        private readonly IEnumerator<decimal> _enumerator;
        private readonly int _periods;
        private readonly Queue<decimal> _queue;

        public MovingSumIterator(IEnumerable<decimal> source, int periods)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _periods = periods > 0 ? periods : throw new ArgumentOutOfRangeException(nameof(periods));

            _enumerator = _source.GetEnumerator();
            _queue = QueuePool<decimal>.Shared.Get();
            _state = 1;
        }

        public override bool MoveNext()
        {
            switch (_state)
            {
                // sma - first value
                case 1:
                    if (_enumerator.MoveNext())
                    {
                        _current = _enumerator.Current;
                        _queue.Enqueue(_current);
                        _state = 2;

                        return true;
                    }
                    else
                    {
                        _state = -1;

                        return false;
                    }

                // sma - following values
                case 2:
                    if (_enumerator.MoveNext())
                    {
                        if (_queue.Count >= _periods)
                        {
                            _current -= _queue.Dequeue();
                        }

                        var value = _enumerator.Current;
                        _queue.Enqueue(value);
                        _current += value;

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

        protected override Iterator<decimal> Clone() => new MovingSumIterator(_source, _periods);

        protected override void Dispose(bool disposing)
        {
            _state = -1;
            _enumerator.Dispose();

            QueuePool<decimal>.Shared.Return(_queue);

            base.Dispose(disposing);
        }
    }
}