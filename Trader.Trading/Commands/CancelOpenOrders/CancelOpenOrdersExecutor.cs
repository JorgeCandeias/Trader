using Microsoft.Extensions.Logging;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Exceptions;
using Outcompute.Trader.Trading.Providers;

namespace Outcompute.Trader.Trading.Commands.CancelOpenOrders;

internal partial class CancelOpenOrdersExecutor : IAlgoCommandExecutor<CancelOpenOrdersCommand>
{
    private readonly ILogger _logger;
    private readonly ITradingService _trader;
    private readonly IOrderProvider _orders;

    public CancelOpenOrdersExecutor(ILogger<CancelOpenOrdersExecutor> logger, ITradingService trader, IOrderProvider orders)
    {
        _logger = logger;
        _trader = trader;
        _orders = orders;
    }

    private const string TypeName = nameof(CancelOpenOrdersExecutor);

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
            CancelStandardOrderResult cancelled;
            try
            {
                cancelled = await _trader
                    .CancelOrderAsync(command.Symbol.Name, order.OrderId, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (UnknownOrderException ex)
            {
                LogCouldNotCancelOrder(ex, TypeName, context.Name, command.Symbol.Name, order.OrderId);

                cancelled = new CancelStandardOrderResult(
                    order.Symbol,
                    order.ClientOrderId,
                    order.OrderId,
                    order.OrderListId,
                    order.ClientOrderId,
                    order.Price,
                    order.OriginalQuantity,
                    order.ExecutedQuantity,
                    order.CummulativeQuoteQuantity,
                    OrderStatus.Canceled,
                    order.TimeInForce,
                    order.Type,
                    order.Side);
            }

            // save the order now to ensure consistency
            await _orders
                .SetOrderAsync(cancelled, cancellationToken)
                .ConfigureAwait(false);

            // update the context spot amounts to allow other commands to execute immediately
        }
    }

    [LoggerMessage(3, LogLevel.Error, "{Type} {Name} {Symbol} could not cancel unknown order {OrderId}")]
    private partial void LogCouldNotCancelOrder(Exception ex, string type, string name, string symbol, long orderId);
}