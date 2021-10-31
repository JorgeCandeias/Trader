using System.Linq;

namespace System.Collections.Generic
{
    public static class RmaExtensions
    {
        public static IEnumerable<decimal> Rma(this IEnumerable<decimal> items, int periods)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));
            if (periods < 1) throw new ArgumentOutOfRangeException(nameof(periods));

            return RmaCore(items, x => x, periods);
        }

        public static IEnumerable<decimal> Rma<T>(this IEnumerable<T> items, Func<T, decimal> accessor, int periods)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));
            if (accessor is null) throw new ArgumentNullException(nameof(accessor));
            if (periods < 1) throw new ArgumentOutOfRangeException(nameof(periods));

            return RmaCore(items, accessor, periods);
        }

        private static IEnumerable<decimal> RmaCore<T>(IEnumerable<T> items, Func<T, decimal> accessor, int periods)
        {
            return new RmaEnumerable(items.Select(x => accessor(x)), periods);

            /*
            var count = 0;
            var enumerator = items.GetEnumerator();
            */

            /*
            // yield the sma until periods - 1
            var sum = 0m;
            var avg = 0m;
            while (count < periods && enumerator.MoveNext())
            {
                ++count;
                sum += accessor(enumerator.Current);
                avg = sum / count;
                yield return avg;
            }

            // yield the rma for the rest
            var rma = avg;
            while (enumerator.MoveNext())
            {
                ++count;

                var current = accessor(enumerator.Current);
                var n = Math.Max(count, periods);
                rma = (((periods - 1) * rma) + current) / n;
                yield return rma;
            }
            */

            /*
            if (enumerator.MoveNext())
            {
                ++count;
                var rma = accessor(enumerator.Current);
                yield return rma;

                while (enumerator.MoveNext())
                {
                    ++count;

                    var current = accessor(enumerator.Current);
                    var n = Math.Min(count, periods);
                    //var n = periods;
                    rma = (((n - 1) * rma) + current) / n;
                    yield return rma;
                }
            }
            */
        }

        private sealed class RmaEnumerable : IEnumerable<decimal>
        {
            private readonly IEnumerable<decimal> _source;
            private readonly int _periods;

            public RmaEnumerable(IEnumerable<decimal> source, int periods)
            {
                _source = source;
                _periods = periods;
            }

            public IEnumerator<decimal> GetEnumerator() => new RmaEnumerator(_source, _periods);

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class RmaEnumerator : IEnumerator<decimal>
            {
                private readonly IEnumerable<decimal> _enumerable;
                private readonly int _periods;

                public RmaEnumerator(IEnumerable<decimal> enumerable, int periods)
                {
                    _enumerable = enumerable;
                    _periods = periods;
                }

                private IEnumerator<decimal>? _enumerator;
                private int _count;

                public decimal Current { get; private set; }

                object IEnumerator.Current => throw new NotImplementedException();

                public void Dispose()
                {
                    _enumerator?.Dispose();
                }

                public bool MoveNext()
                {
                    _enumerator ??= _enumerable.GetEnumerator();

                    if (!_enumerator.MoveNext())
                    {
                        return false;
                    }

                    if (_count == 0)
                    {
                        _count++;
                        Current = _enumerator.Current;
                    }
                    else
                    {
                        _count++;

                        var last = _enumerator.Current;
                        //var n = Math.Min(_count, _periods);
                        var n = _periods;
                        //var n = _count;
                        Current = (((n - 1) * Current) + last) / n;
                    }

                    return true;
                }

                public void Reset()
                {
                    _enumerator = _enumerable.GetEnumerator();
                    _count = 0;
                }
            }
        }
    }
}