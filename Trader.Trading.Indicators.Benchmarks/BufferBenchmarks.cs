using BenchmarkDotNet.Attributes;
using System;
using System.Buffers;

namespace Trader.Trading.Indicators.Benchmarks
{
    [MemoryDiagnoser]
    public class BufferBenchmarks
    {
        [Params(1000)]
        public int N { get; set; }

        [Benchmark]
        public void Stackallock()
        {
            Span<int> buffer = stackalloc int[N];

            for (var i = 0; i < N; i++)
            {
                buffer[i] = i;
            }
        }

        [Benchmark]
        public void ArrayPool()
        {
            var buffer = ArrayPool<int>.Shared.Rent(N);
            var segment = new ArraySegment<int>(buffer, 0, N);

            for (var i = 0; i < N; i++)
            {
                segment[i] = i;
            }

            ArrayPool<int>.Shared.Return(buffer);
        }

        [Benchmark]
        public void MemoryPoolPreSlice()
        {
            using var buffer = MemoryPool<int>.Shared.Rent(N);
            var span = buffer.Memory.Slice(0, N).Span;

            for (var i = 0; i < N; i++)
            {
                span[i] = i;
            }
        }

        [Benchmark]
        public void MemoryPoolPostSlice()
        {
            using var buffer = MemoryPool<int>.Shared.Rent(N);
            var span = buffer.Memory.Span.Slice(0, N);

            for (var i = 0; i < N; i++)
            {
                span[i] = i;
            }
        }
    }
}