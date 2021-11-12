using Outcompute.Trader.Trading.Commands;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    /// <summary>
    /// An empty algo that does nothing.
    /// For use with non nullable fields and unit testing.
    /// </summary>
    public class NoopAlgo : Algo
    {
        private NoopAlgo()
        {
        }

        public static NoopAlgo Instance { get; } = new NoopAlgo();

        protected override ValueTask<IAlgoCommand> OnExecuteAsync(CancellationToken cancellationToken = default)
        {
            return Noop().AsValueTaskResult<IAlgoCommand>();
        }
    }
}