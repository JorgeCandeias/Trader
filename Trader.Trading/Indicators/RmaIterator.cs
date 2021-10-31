﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Outcompute.Trader.Trading.Indicators
{
    /// <summary>
    /// An iterator that calculates the RMA over the specified source.
    /// </summary>
    internal sealed class RmaIterator : Iterator<decimal>
    {
        private readonly IEnumerable<decimal> _source;
        private readonly int _periods;

        private IEnumerator<decimal>? _enumerator;

        public RmaIterator(IEnumerable<decimal> source, int periods)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _periods = periods > 0 ? periods : throw new ArgumentOutOfRangeException(nameof(periods));
        }

        [SuppressMessage("Major Code Smell", "S907:\"goto\" statement should not be used", Justification = "Lazy Iterator Optimization")]
        public override bool MoveNext()
        {
            switch (_state)
            {
                // lazily initialize the enumerator
                case 0:
                    _enumerator = _source.GetEnumerator();
                    _state = 1;
                    goto case 1;

                // rma - first value
                case 1:
                    if (_enumerator!.MoveNext())
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
                    if (_enumerator!.MoveNext())
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

            if (_enumerator is not null)
            {
                _enumerator.Dispose();
                _enumerator = null;
            }

            base.Dispose(disposing);
        }
    }
}