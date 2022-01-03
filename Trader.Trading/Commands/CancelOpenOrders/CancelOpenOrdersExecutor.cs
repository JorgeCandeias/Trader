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
        foreach (var order in context.Data[command.Symbol.Name].Orders.Open)
        {
            // apply the side rule
            if (command.Side.HasValue && command.Side.Value != order.Side)
            {
                continue;
            }

            // apply the distance rule
            if (command.Distance.HasValue)
            {
                var ticker = context.Data[command.Symbol.Name].Ticker;

                var distance = Math.Abs((order.Price - ticker.ClosePrice) / ticker.ClosePrice);
                if (distance <= command.Distance.Value)
                {
                    continue;
                }
            }

            // apply the tag rule
            if (command.Tag is not null && order.ClientOrderId != command.Tag)
            {
                continue;
            }

            // cancel the identified order
            var cancelled = await _trader
                .CancelOrderAsync(command.Symbol.Name, order.OrderId, cancellationToken)
                .ConfigureAwait(false);

            // save the order now to ensure consistency
            await _orders
                .SetOrderAsync(cancelled, cancellationToken)
                .ConfigureAwait(false);

            // update the context spot amounts to allow other commands to execute immediately
            

        }
    }
}