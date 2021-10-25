using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Commands;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Commands.AveragingSell
{
    internal class AveragingSellExecutor : IAlgoCommandExecutor<AveragingSellCommand>
    {
        private readonly IAveragingSellService _block;

        public AveragingSellExecutor(IAveragingSellService block)
        {
            _block = block;
        }

        public Task ExecuteAsync(IAlgoContext context, AveragingSellCommand result, CancellationToken cancellationToken = default)
        {
            return _block.SetAveragingSellAsync(result.Symbol, result.Orders, result.ProfitMultiplier, result.RedeemSavings, cancellationToken);
        }
    }
}