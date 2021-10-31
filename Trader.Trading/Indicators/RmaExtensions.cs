namespace System.Collections.Generic
{
    public static class RmaExtensions
    {
        /// <summary>
        /// Calculates the Running Moving Average over the specified source.
        /// </summary>
        /// <param name="items">The source for RMA calculation.</param>
        /// <param name="periods">The number of periods for RMA calculation.</param>
        /// <returns>An enumerable that calculates the Running Moving Average over the specified source when enumerated.</returns>
        public static IEnumerable<decimal> Rma(this IEnumerable<decimal> items, int periods)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));
            if (periods < 0) throw new ArgumentOutOfRangeException(nameof(periods));

            return RmaCore(items, PassthroughDelegate, periods);
        }

        /// <inheritdoc cref="Rma(IEnumerable{decimal}, int)"/>
        /// <param name="accessor">A transform function to apply to each element.</param>
        public static IEnumerable<decimal> Rma<T>(this IEnumerable<T> items, Func<T, decimal> accessor, int periods)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));
            if (accessor is null) throw new ArgumentNullException(nameof(accessor));
            if (periods < 0) throw new ArgumentOutOfRangeException(nameof(periods));

            return RmaCore(items, accessor, periods);
        }

        /// <summary>
        /// Creates a new <see cref="RmaEnumerable{T}"/> instance.
        /// </summary>
        private static IEnumerable<decimal> RmaCore<T>(IEnumerable<T> items, Func<T, decimal> accessor, int periods)
        {
            return new RmaEnumerable<T>(items, accessor, periods);
        }

        /// <summary>
        /// Caches the passthrough delegate used by <see cref="Rma(IEnumerable{decimal}, int)"/> to avoid redundant allocations.
        /// </summary>
        private static readonly Func<decimal, decimal> PassthroughDelegate = Passthrough;

        /// <summary>
        /// A passthrough transform function used by <see cref="Rma(IEnumerable{decimal}, int)"/>.
        /// </summary>
        private static decimal Passthrough(decimal value) => value;

        /// <summary>
        /// An allocation-optimized enumerable that calculates the RMA over the specified source when enumerated.
        /// </summary>
        private sealed class RmaEnumerable<T> : IEnumerable<decimal>
        {
            private readonly IEnumerable<T> _source;
            private readonly Func<T, decimal> _accessor;
            private readonly int _periods;

            public RmaEnumerable(IEnumerable<T> source, Func<T, decimal> accessor, int periods)
            {
                _source = source;
                _accessor = accessor;
                _periods = periods;
            }

            public IEnumerator<decimal> GetEnumerator() => new RmaEnumerator(_source, _accessor, _periods);

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            /// <summary>
            /// An allocation-optimized enumerator for <see cref="RmaEnumerable{T}"/>.
            /// </summary>
            private sealed class RmaEnumerator : IEnumerator<decimal>
            {
                private readonly IEnumerable<T> _source;
                private readonly Func<T, decimal> _accessor;
                private readonly int _periods;

                public RmaEnumerator(IEnumerable<T> source, Func<T, decimal> accessor, int periods)
                {
                    _source = source;
                    _accessor = accessor;
                    _periods = periods;

                    _enumerator = _source.GetEnumerator();
                    _action = _phaseOne;
                }

                private IEnumerator<T> _enumerator;
                private decimal? _current;

                public decimal Current => _current ?? throw new InvalidOperationException();

                object IEnumerator.Current => Current;

                private Action<RmaEnumerator, decimal> _action;

                private static readonly Action<RmaEnumerator, decimal> _phaseOne = PhaseOne;

                private static void PhaseOne(RmaEnumerator myself, decimal last)
                {
                    myself._current = last;
                    myself._action = _phaseTwo;
                }

                private static readonly Action<RmaEnumerator, decimal> _phaseTwo = PhaseTwo;

                private static void PhaseTwo(RmaEnumerator myself, decimal last)
                {
                    myself._current = (((myself._periods - 1) * myself._current!.Value) + last) / myself._periods;
                }

                public bool MoveNext()
                {
                    if (_enumerator.MoveNext())
                    {
                        var last = _accessor(_enumerator.Current);

                        _action(this, last);

                        return true;
                    }
                    else
                    {
                        _current = null;

                        return false;
                    }
                }

                public void Reset()
                {
                    _enumerator.Dispose();
                    _enumerator = _source.GetEnumerator();
                    _action = _phaseOne;
                    _current = null;
                }

                public void Dispose()
                {
                    _enumerator.Dispose();
                }
            }
        }
    }
}