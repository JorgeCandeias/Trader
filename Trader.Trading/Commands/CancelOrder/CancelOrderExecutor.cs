using Microsoft.Extensions.Logging;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Exceptions;
using Outcompute.Trader.Trading.Providers;
using System.Diagnostics;

namespace Outcompute.Trader.Trading.Commands.CancelOrder;

internal partial class CancelOrderExecutor : IAlgoCommandExecutor<CancelOrderCommand>
{
    private readonly ILogger _logger;
    private readonly ITradingService _trader;
    private readonly IOrderProvider _orders;

    public CancelOrderExecutor(ILogger<CancelOrderExecutor> logger, ITradingService trader, IOrderProvider orders)
    {
        _logger = logger;
        _trader = trader;
        _orders = orders;
    }

    private static string TypeName => nameof(CancelOrderExecutor);

    public async ValueTask ExecuteAsync(IAlgoContext context, CancelOrderCommand command, CancellationToken cancellationToken = default)
    {
        LogStart(TypeName, context.Name, command.Symbol.Name, command.OrderId);

        var watch = Stopwatch.StartNew();

        CancelStandardOrderResult order;
        try
        {
            order = await _trader
                .CancelOrderAsync(command.Symbol.Name, command.OrderId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (UnknownOrderException ex)
        {
            LogCouldNotCancelOrder(ex, TypeName, context.Name, command.Symbol.Name, command.OrderId);
            return;
        }

        await _orders
            .SetOrderAsync(order, cancellationToken)
            .ConfigureAwait(false);

        // adjust the spot balance for the current context so subsequent commands can attempt to execute
        var data = context.Data[command.Symbol.Name];
        var cancelled = data.Orders.Open.FirstOrDefault(x => x.OrderId == command.OrderId);
        if (cancelled is not null)
        {
            switch (cancelled.Side)
            {
                case OrderSide.Buy:
                    var quantity = cancelled.OriginalQuantity * cancelled.Price;
                    data.Spot.QuoteAsset = data.Spot.QuoteAsset with
                    {
                        Free = data.Spot.QuoteAsset.Free + quantity,
                        Locked = data.Spot.QuoteAsset.Locked - quantity
                    };
                    break;

                case OrderSide.Sell:
                    data.Spot.BaseAsset = data.Spot.BaseAsset with
                    {
                        Free = data.Spot.BaseAsset.Free + cancelled.OriginalQuantity,
                        Locked = data.Spot.BaseAsset.Locked - cancelled.OriginalQuantity
                    };
                    break;
            }
        }

        LogEnd(TypeName, context.Name, command.Symbol.Name, command.OrderId, watch.ElapsedMilliseconds);
    }

    #region Logging

    [LoggerMessage(1, LogLevel.Information, "{Type} {Name} {Symbol} cancelling order {OrderId}")]
    private partial void LogStart(string type, string name, string symbol, long orderId);

    [LoggerMessage(2, LogLevel.Information, "{Type} {Name} {Symbol} cancelled order {OrderId} in {ElapsedMs}ms")]
    private partial void LogEnd(string type, string name, string symbol, long orderId, long elapsedMs);

    [LoggerMessage(3, LogLevel.Error, "{Type} {Name} {Symbol} could not cancel unknown order {OrderId}")]
    private partial void LogCouldNotCancelOrder(Exception ex, string type, string name, string symbol, long orderId);

    #endregion Logging
}