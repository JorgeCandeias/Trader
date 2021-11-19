using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Outcompute.Trader.Core.Pooling;
using System.Collections.Generic;

namespace Trader.Trading.Indicators.Benchmarks
{
    [MemoryDiagnoser]
    public class QueuePoolBenchmarks
    {
        private readonly Consumer _consumer = new();

        private void Work(Queue<int> queue)
        {
            for (var i = 0; i < N; i++)
            {
                queue.Enqueue(i);
            }

            while (queue.TryDequeue(out var result))
            {
                _consumer.Consume(result);
            }
        }

        [Params(10, 100, 1000, 10000)]
        public int N { get; set; }

        [Benchmark(Baseline = true)]
        public void Queue()
        {
            var queue = new Queue<int>();

            Work(queue);
        }

        [Benchmark]
        public void QueuePool()
        {
            var queue = QueuePool<int>.Shared.Get();

            Work(queue);

            QueuePool<int>.Shared.Return(queue);
        }
    }
}