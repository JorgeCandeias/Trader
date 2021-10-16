using BenchmarkDotNet.Attributes;
using System;

namespace Trader.Trading.Indicators.Benchmarks
{
    [MemoryDiagnoser]
    public class ArrayEmptyBenchmarks
    {
        [Benchmark]
        public ArrayEmptyBenchmarks[] ArrayEmpty()
        {
            return Array.Empty<ArrayEmptyBenchmarks>();
        }
    }
}