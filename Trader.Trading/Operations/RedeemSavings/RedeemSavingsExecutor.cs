using Outcompute.Trader.Trading.Algorithms;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Operations.RedeemSavings
{
    internal class RedeemSavingsExecutor : IAlgoResultExecutor<RedeemSavingsAlgoResult>
    {
        private readonly IRedeemSavingsOperation _operation;

        public RedeemSavingsExecutor(IRedeemSavingsOperation operation)
        {
            _operation = operation;
        }

        public Task ExecuteAsync(IAlgoContext context, RedeemSavingsAlgoResult result, CancellationToken cancellationToken = default)
        {
            return _operation.TryRedeemSavingsAsync(result.Asset, result.Amount, cancellationToken);
        }
    }
}