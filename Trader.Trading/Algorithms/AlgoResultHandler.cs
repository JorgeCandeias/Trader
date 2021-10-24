using Outcompute.Trader.Trading.Algorithms;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Blocks
{
    internal class AlgoResultHandler : IAlgoResultHandler
    {
        public Task HandleAsync(IAlgoResult result)
        {
            // todo
            return Task.CompletedTask;
        }
    }
}