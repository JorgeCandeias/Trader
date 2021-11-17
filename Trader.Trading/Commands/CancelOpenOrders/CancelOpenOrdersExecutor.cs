using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Providers;

namespace Outcompute.Trader.Trading.Commands.CancelOpenOrders;

internal class CancelOpenOrdersExecutor : IAlgoCommandExecutor<CancelOpenOrdersCommand>
{
    private readonly ITradingService _trader;
    private readonly IOrderProvider _orders;

    public CancelOpenOrdersExecutor(ITradingService trader, IOrderProvider orders)
    {
        _trader = trader;
        _orders = orders;
    }

    public async ValueTask ExecuteAsync(IAlgoContext context, CancelOpenOrdersCommand command, CancellationToken cancellationToken = default)
    {
        var ticker = context.Data[command.Symbol.Name].Ticker;

        var orders = await _orders
            .GetOrdersByFilterAsync(command.Symbol.Name, command.Side, true, null, cancellationToken)
            .ConfigureAwait(false);

        foreach (var order in orders)
        {
            // evaluate the distance rule
            if (command.Distance.HasValue)
            {
                var distance = Math.Abs((order.Price - ticker.ClosePrice) / ticker.ClosePrice);
                if (distance <= command.Distance.Value)
                {
                    continue;
                }
            }

            var cancelled = await _trader
                .CancelOrderAsync(command.Symbol.Name, order.OrderId, cancellationToken)
                .ConfigureAwait(false);

            await _orders
                .SetOrderAsync(cancelled, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}