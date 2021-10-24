using Outcompute.Trader.Trading.Algorithms;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Operations.AveragingSell
{
    internal class AveragingSellExecutor : IAlgoResultExecutor<AveragingSellAlgoResult>
    {
        private readonly IAveragingSellOperation _block;

        public AveragingSellExecutor(IAveragingSellOperation block)
        {
            _block = block;
        }

        public Task ExecuteAsync(IAlgoContext context, AveragingSellAlgoResult result, CancellationToken cancellationToken = default)
        {
            return _block.SetAveragingSellAsync(result.Symbol, result.Orders, result.ProfitMultiplier, result.RedeemSavings, cancellationToken);
        }
    }
}