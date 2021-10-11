using BenchmarkDotNet.Attributes;
using System.Collections.Immutable;

namespace Trader.Trading.Indicators.Benchmarks
{
    [MemoryDiagnoser]
    public class ImmutableSortedSetBuilderBenchmark
    {
        private readonly ImmutableSortedSet<int>.Builder _builder = ImmutableSortedSet.CreateBuilder<int>();

        public ImmutableSortedSetBuilderBenchmark()
        {
            _builder.UnionWith(new[] { 1, 2, 3 });
        }

        [Benchmark]
        public ImmutableSortedSet<int> BuilderWithNoChanges()
        {
            return _builder.ToImmutable();
        }

        [Benchmark]
        public ImmutableSortedSet<int> BuilderWithOneChange()
        {
            _builder.Add(4);

            return _builder.ToImmutable();
        }

        [Benchmark]
        public ImmutableSortedSet<int> BuilderWithTwoChanges()
        {
            _builder.Add(4);
            _builder.Add(5);

            return _builder.ToImmutable();
        }

        [Benchmark]
        public ImmutableSortedSet<int> BuilderWithThreeChanges()
        {
            _builder.Add(4);
            _builder.Add(5);
            _builder.Add(6);

            return _builder.ToImmutable();
        }

        [Benchmark]
        public ImmutableSortedSet<int> BuilderWithClear()
        {
            _builder.Clear();

            return _builder.ToImmutable();
        }

        [Benchmark]
        public ImmutableSortedSet<int> BuilderWithReset()
        {
            _builder.Clear();
            _builder.Add(1);
            _builder.Add(2);
            _builder.Add(3);

            return _builder.ToImmutable();
        }
    }
}