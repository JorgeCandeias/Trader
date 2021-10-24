using Outcompute.Trader.Trading.Algorithms;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Operations.ClearOpenOrders
{
    internal class ClearOpenOrdersExecutor : IAlgoResultExecutor<ClearOpenOrdersAlgoResult>
    {
        private readonly IClearOpenOrdersOperation _operation;

        public ClearOpenOrdersExecutor(IClearOpenOrdersOperation operation)
        {
            _operation = operation;
        }

        public Task ExecuteAsync(IAlgoContext context, ClearOpenOrdersAlgoResult result, CancellationToken cancellationToken = default)
        {
            return _operation.ClearOpenOrdersAsync(result.Symbol, result.Side, cancellationToken);
        }
    }
}