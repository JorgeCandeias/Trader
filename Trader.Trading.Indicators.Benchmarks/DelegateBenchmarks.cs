using BenchmarkDotNet.Attributes;
using System;

namespace Trader.Trading.Indicators.Benchmarks
{
    [MemoryDiagnoser]
    public class DelegateBenchmarks
    {
        public DelegateBenchmarks()
        {
            _executee = SomeExecutee;
        }

        private int _value;

        private readonly Action _executee;

        private void SomeExecutee()
        {
            unchecked
            {
                _value++;
            }
        }

        private void SomeExecutor(Action action)
        {
            action();
        }

        [Benchmark]
        public void InPlaceDelegate()
        {
            SomeExecutor(SomeExecutee);
        }

        [Benchmark]
        public void CachedDelegate()
        {
            SomeExecutor(_executee);
        }
    }
}