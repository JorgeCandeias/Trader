using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Trader.Trading.Indicators.Benchmarks
{
    [MemoryDiagnoser]
    public class DictionaryEnumerationBenchmarks
    {
        private readonly Consumer _consumer = new();

        private readonly Dictionary<string, string> _data = Enumerable.Range(1, 1000000).ToDictionary(_ => Guid.NewGuid().ToString(), _ => Guid.NewGuid().ToString());

        [Benchmark]
        public void EnumerateDictionary()
        {
            _data.Consume(_consumer);
        }

        [Benchmark]
        public void EnumerateKeys()
        {
            _data.Keys.Consume(_consumer);
        }

        [Benchmark]
        public void EnumerateValues()
        {
            _data.Values.Consume(_consumer);
        }
    }
}