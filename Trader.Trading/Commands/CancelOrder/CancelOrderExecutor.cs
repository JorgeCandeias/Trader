using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Commands;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Commands.CancelOrder
{
    internal class CancelOrderExecutor : IAlgoCommandExecutor<CancelOrderCommand>
    {
        private readonly ICancelOrderService _operation;

        public CancelOrderExecutor(ICancelOrderService operation)
        {
            _operation = operation;
        }

        public Task ExecuteAsync(IAlgoContext context, CancelOrderCommand result, CancellationToken cancellationToken = default)
        {
            return _operation.CancelOrderAsync(result.Symbol, result.OrderId, cancellationToken);
        }
    }
}