using BenchmarkDotNet.Attributes;
using System.Threading.Tasks;

namespace Trader.Trading.Indicators.Benchmarks
{
    [MemoryDiagnoser]
    public class TaskBenchmarks
    {
        /*
        [Benchmark]
        public Task ReturnCompletedTask()
        {
            return Task.CompletedTask;
        }

        [Benchmark]
        public ValueTask ReturnCompletedValueTask()
        {
            return ValueTask.CompletedTask;
        }

        [Benchmark]
        public Task ReturnYieldTask()
        {
            return TaskYield();
        }

        [Benchmark]
        public ValueTask ReturnValueYieldTask()
        {
            return new ValueTask(TaskYield());
        }

        [Benchmark]
        public async Task AwaitCompletedTask()
        {
            await Task.CompletedTask.ConfigureAwait(false);
        }

        [Benchmark]
        public async ValueTask AwaitCompletedValueTask()
        {
            await ValueTask.CompletedTask.ConfigureAwait(false);
        }

        [Benchmark]
        public async Task AwaitAsyncTask()
        {
            await Task.Yield();
        }
        */

        [Benchmark]
        public int ReturnZero()
        {
            return _zero;
        }

        [Benchmark]
        public int ReturnRandom()
        {
            return Next();
        }

        [Benchmark]
        public async Task<int> AwaitZero()
        {
            await Task.CompletedTask.ConfigureAwait(false);

            return _zero;
        }

        [Benchmark]
        public async Task<int> AwaitRandom()
        {
            await Task.CompletedTask.ConfigureAwait(false);

            return Next();
        }

        [Benchmark]
        public async ValueTask<int> AwaitValueZero()
        {
            await Task.CompletedTask.ConfigureAwait(false);

            return _zero;
        }

        [Benchmark]
        public async ValueTask<int> AwaitValueRandom()
        {
            await Task.CompletedTask.ConfigureAwait(false);

            return Next();
        }

        private async Task TaskYield()
        {
            await Task.Yield();
        }

        private int _count;

        private const int _zero = 0;

        private int Next()
        {
            unchecked
            {
                return ++_count;
            }
        }
    }
}