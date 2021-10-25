using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Commands;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Commands.RedeemSavings
{
    internal class RedeemSavingsExecutor : IAlgoCommandExecutor<RedeemSavingsCommand>
    {
        private readonly IRedeemSavingsService _operation;

        public RedeemSavingsExecutor(IRedeemSavingsService operation)
        {
            _operation = operation;
        }

        public Task ExecuteAsync(IAlgoContext context, RedeemSavingsCommand result, CancellationToken cancellationToken = default)
        {
            return _operation.TryRedeemSavingsAsync(result.Asset, result.Amount, cancellationToken);
        }
    }
}