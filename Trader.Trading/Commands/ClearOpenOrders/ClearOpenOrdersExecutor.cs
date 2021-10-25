using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Commands;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Commands.ClearOpenOrders
{
    internal class ClearOpenOrdersExecutor : IAlgoCommandExecutor<ClearOpenOrdersCommand>
    {
        private readonly IClearOpenOrdersService _operation;

        public ClearOpenOrdersExecutor(IClearOpenOrdersService operation)
        {
            _operation = operation;
        }

        public Task ExecuteAsync(IAlgoContext context, ClearOpenOrdersCommand result, CancellationToken cancellationToken = default)
        {
            return _operation.ClearOpenOrdersAsync(result.Symbol, result.Side, cancellationToken);
        }
    }
}