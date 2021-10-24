using Outcompute.Trader.Trading.Algorithms;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Blocks.AveragingSell
{
    internal class AveragingSellExecutor : IAlgoResultExecutor<AveragingSellResult>
    {
        private readonly IAveragingSellBlock _block;

        public AveragingSellExecutor(IAveragingSellBlock block)
        {
            _block = block;
        }

        public Task ExecuteAsync(IAlgoContext context, AveragingSellResult result, CancellationToken cancellationToken = default)
        {
            return _block.SetAveragingSellAsync(result.Symbol, result.Orders, result.ProfitMultiplier, result.RedeemSavings, cancellationToken);
        }
    }
}