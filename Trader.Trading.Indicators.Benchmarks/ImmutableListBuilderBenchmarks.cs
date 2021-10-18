using BenchmarkDotNet.Attributes;
using System.Collections.Immutable;

namespace Trader.Trading.Indicators.Benchmarks
{
    public class ImmutableListBuilderBenchmarks
    {
        [Benchmark]
        public ImmutableList<int>.Builder CreateBuilder()
        {
            return ImmutableList.CreateBuilder<int>();
        }
    }
}