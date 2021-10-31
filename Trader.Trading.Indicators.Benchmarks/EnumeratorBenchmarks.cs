using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Trader.Trading.Indicators.Benchmarks
{
    [MemoryDiagnoser]
    public class EnumeratorBenchmarks
    {
        private readonly Consumer _consumer = new();

        [Params(10, 100, 1000)]
        public int N { get; set; }

        [Benchmark(Baseline = true)]
        public void EnumerateYieldMethod()
        {
            EnumerateWithYieldReturn(N).Consume(_consumer);
        }

        [Benchmark]
        public void EnumerateEnumerableClass()
        {
            EnumerateWithClass(N).Consume(_consumer);
        }

        [Benchmark]
        public void EnumerateIteratorClass()
        {
            EnumerateWithIteratorClass(N).Consume(_consumer);
        }

        [Benchmark]
        public void EnumerateActionIteratorClass()
        {
            EnumerateWithActionIteratorClass(N).Consume(_consumer);
        }

        private static IEnumerable<int> EnumerateWithYieldReturn(int count)
        {
            for (var i = 0; i < count; i++)
            {
                yield return i;
            }
        }

        private static IEnumerable<int> EnumerateWithClass(int count)
        {
            return new EnumerableClass(count);
        }

        private static IEnumerable<int> EnumerateWithIteratorClass(int count)
        {
            return new IteratorClass(count);
        }

        private static IEnumerable<int> EnumerateWithActionIteratorClass(int count)
        {
            return new ActionIteratorClass(count);
        }

        private sealed class EnumerableClass : IEnumerable<int>
        {
            private readonly int _count;

            public EnumerableClass(int count)
            {
                _count = count;
            }

            public IEnumerator<int> GetEnumerator() => new EnumeratorClass(this);

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class EnumeratorClass : IEnumerator<int>
            {
                private readonly EnumerableClass _enumerable;

                public EnumeratorClass(EnumerableClass enumerable)
                {
                    _enumerable = enumerable;
                }

                private int _current = -1;
                private int _state;

                public int Current => _state is 1 ? _current : throw new InvalidOperationException();

                object IEnumerator.Current => Current;

                public void Dispose()
                {
                    // noop
                }

                public bool MoveNext()
                {
                    switch (_state)
                    {
                        case 0:
                            {
                                var next = _current + 1;
                                if (next < _enumerable._count)
                                {
                                    _current = next;
                                    _state = 1;
                                    return true;
                                }
                                else
                                {
                                    _current = -1;
                                    _state = 2;
                                    return false;
                                }
                            }

                        case 1:
                            {
                                var next = _current + 1;
                                if (next < _enumerable._count)
                                {
                                    _current = next;
                                    return true;
                                }
                                else
                                {
                                    _current = -1;
                                    _state = 2;
                                    return false;
                                }
                            }
                    }

                    return false;
                }

                public void Reset()
                {
                    _current = -1;
                    _state = 0;
                }
            }
        }

        private sealed class IteratorClass : IEnumerable<int>, IEnumerator<int>
        {
            private readonly int _count;

            private bool _used;
            private int _current = -1;
            private int _state;

            public IteratorClass(int count)
            {
                _count = count;
            }

            public IEnumerator<int> GetEnumerator()
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

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private IteratorClass Clone() => new(_count);

            public int Current => _state is 1 ? _current : throw new InvalidOperationException();

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                // noop
            }

            public bool MoveNext()
            {
                switch (_state)
                {
                    case 0:
                        {
                            var next = _current + 1;
                            if (next < _count)
                            {
                                _current = next;
                                _state = 1;
                                return true;
                            }
                            else
                            {
                                _current = -1;
                                _state = 2;
                                return false;
                            }
                        }

                    case 1:
                        {
                            var next = _current + 1;
                            if (next < _count)
                            {
                                _current = next;
                                return true;
                            }
                            else
                            {
                                _current = -1;
                                _state = 2;
                                return false;
                            }
                        }
                }

                return false;
            }

            public void Reset()
            {
                _current = -1;
                _state = 0;
            }
        }

        private sealed class ActionIteratorClass : IEnumerable<int>, IEnumerator<int>
        {
            private readonly int _count;

            private bool _used;
            private int _current = -1;
            private Func<ActionIteratorClass, bool> _state;

            public ActionIteratorClass(int count)
            {
                _count = count;
                _state = _state0;
            }

            public IEnumerator<int> GetEnumerator()
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

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private IteratorClass Clone() => new(_count);

            public int Current => _state == _state1 ? _current : throw new InvalidOperationException();

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                // noop
            }

            public bool MoveNext()
            {
                return _state(this);
            }

            public void Reset()
            {
                _current = -1;
                _state = _state0;
            }

            private static readonly Func<ActionIteratorClass, bool> _state0 = State0;
            private static readonly Func<ActionIteratorClass, bool> _state1 = State1;
            private static readonly Func<ActionIteratorClass, bool> _state2 = State2;

            private static bool State0(ActionIteratorClass iterator)
            {
                var next = iterator._current + 1;
                if (next < iterator._count)
                {
                    iterator._current = next;
                    iterator._state = _state1;

                    return true;
                }
                else
                {
                    iterator._current = -1;
                    iterator._state = _state2;

                    return false;
                }
            }

            private static bool State1(ActionIteratorClass iterator)
            {
                var next = iterator._current + 1;
                if (next < iterator._count)
                {
                    iterator._current = next;

                    return true;
                }
                else
                {
                    iterator._current = -1;
                    iterator._state = _state2;

                    return false;
                }
            }

            private static bool State2(ActionIteratorClass iterator)
            {
                return false;
            }
        }
    }
}