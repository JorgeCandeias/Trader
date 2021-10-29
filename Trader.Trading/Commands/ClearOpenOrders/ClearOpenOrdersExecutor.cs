using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Providers;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Commands.ClearOpenOrders
{
    internal class ClearOpenOrdersExecutor : IAlgoCommandExecutor<ClearOpenOrdersCommand>
    {
        private readonly ITradingService _trader;
        private readonly IOrderProvider _orders;

        public ClearOpenOrdersExecutor(ITradingService trader, IOrderProvider orders)
        {
            _trader = trader;
            _orders = orders;
        }

        public async Task ExecuteAsync(IAlgoContext context, ClearOpenOrdersCommand command, CancellationToken cancellationToken = default)
        {
            var orders = await _orders
                .GetOrdersByFilterAsync(command.Symbol.Name, command.Side, true, cancellationToken)
                .ConfigureAwait(false);

            foreach (var order in orders)
            {
                var cancelled = await _trader
                    .CancelOrderAsync(command.Symbol.Name, order.OrderId, cancellationToken)
                    .ConfigureAwait(false);

                await _orders
                    .SetOrderAsync(cancelled, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}