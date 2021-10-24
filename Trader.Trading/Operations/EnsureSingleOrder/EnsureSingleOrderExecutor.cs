using Outcompute.Trader.Trading.Algorithms;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Operations.EnsureSingleOrder
{
    internal class EnsureSingleOrderExecutor : IAlgoResultExecutor<EnsureSingleOrderAlgoResult>
    {
        private readonly IEnsureSingleOrderOperation _operation;

        public EnsureSingleOrderExecutor(IEnsureSingleOrderOperation operation)
        {
            _operation = operation;
        }

        public Task ExecuteAsync(IAlgoContext context, EnsureSingleOrderAlgoResult result, CancellationToken cancellationToken = default)
        {
            return _operation.EnsureSingleOrderAsync(result.Symbol, result.Side, result.Type, result.TimeInForce, result.Quantity, result.Price, result.RedeemSavings, cancellationToken);
        }
    }
}