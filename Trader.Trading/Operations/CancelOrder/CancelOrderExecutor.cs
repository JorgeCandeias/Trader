using Outcompute.Trader.Trading.Algorithms;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Operations.CancelOrder
{
    internal class CancelOrderExecutor : IAlgoResultExecutor<CancelOrderResult>
    {
        private readonly ICancelOrderOperation _operation;

        public CancelOrderExecutor(ICancelOrderOperation operation)
        {
            _operation = operation;
        }

        public Task ExecuteAsync(IAlgoContext context, CancelOrderResult result, CancellationToken cancellationToken = default)
        {
            return _operation.CancelOrderAsync(result.Symbol, result.OrderId, cancellationToken);
        }
    }
}