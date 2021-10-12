using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Trader.Trading.Indicators.Benchmarks
{
    [MemoryDiagnoser]
    public class ConcurrentDictionaryEnumerationBenchmarks
    {
        private readonly Consumer _consumer = new();

        private readonly ConcurrentDictionary<string, string> _data = new ConcurrentDictionary<string, string>(Enumerable.Range(1, 1000000).Select(x => new KeyValuePair<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString())));

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
        public void EnumerateSelectKeys()
        {
            _data.Select(x => x.Key).Consume(_consumer);
        }

        [Benchmark]
        public void EnumerateValues()
        {
            _data.Values.Consume(_consumer);
        }

        [Benchmark]
        public void EnumerateSelectValues()
        {
            _data.Select(x => x.Value).Consume(_consumer);
        }
    }
}