using Microsoft.Extensions.Logging;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Exceptions;
using Outcompute.Trader.Trading.Providers;
using System.Diagnostics;

namespace Outcompute.Trader.Trading.Commands.CreateOrder;

internal partial class CreateOrderExecutor : IAlgoCommandExecutor<CreateOrderCommand>
{
    private readonly ILogger _logger;
    private readonly ITradingService _trader;
    private readonly IOrderProvider _orders;

    public CreateOrderExecutor(ILogger<CreateOrderExecutor> logger, ITradingService trader, IOrderProvider orders)
    {
        _logger = logger;
        _trader = trader;
        _orders = orders;
    }

    private static string TypeName { get; } = nameof(CreateOrderExecutor);

    public async ValueTask ExecuteAsync(IAlgoContext context, CreateOrderCommand command, CancellationToken cancellationToken = default)
    {
        var data = context.Data[command.Symbol.Name];
        var ticker = data.Ticker.ClosePrice;

        string asset;
        decimal required;
        decimal free;

        switch (command.Side)
        {
            case OrderSide.Buy:

                asset = command.Symbol.QuoteAsset;
                free = data.Spot.QuoteAsset.Free;

                if (command.Quantity.HasValue)
                {
                    required = command.Quantity.Value * (command.Price ?? ticker);
                }
                else if (command.Notional.HasValue)
                {
                    required = command.Notional.Value;
                }
                else
                {
                    ThrowHelper.ThrowInvalidOperationException();
                    return;
                }

                break;

            case OrderSide.Sell:

                asset = command.Symbol.BaseAsset;
                free = data.Spot.BaseAsset.Free;

                if (command.Quantity.HasValue)
                {
                    required = command.Quantity.Value;
                }
                else if (command.Notional.HasValue)
                {
                    required = command.Notional.Value / (command.Price ?? ticker);
                }
                else
                {
                    ThrowHelper.ThrowInvalidOperationException();
                    return;
                }

                break;

            default:
                ThrowHelper.ThrowInvalidOperationException();
                return;
        }

        if (free < required)
        {
            LogCannotPlaceOrderWithoutFreeQuantity(TypeName, context.Name, command.Type, command.Side, required, asset, free);
            return;
        }

        var watch = Stopwatch.StartNew();

        LogPlacingOrder(TypeName, command.Symbol.Name, command.Type, command.Side, command.Quantity, command.Notional, command.Symbol.BaseAsset, command.Price, command.Symbol.QuoteAsset, command.Notional.HasValue ? command.Notional : command.Quantity * command.Price);

        OrderResult created;
        try
        {
            created = await _trader
                .CreateOrderAsync(command.Symbol.Name, command.Side, command.Type, command.TimeInForce, command.Quantity, command.Notional, command.Price, command.Tag, command.StopPrice, null, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (TraderException ex)
        {
            LogFailedToCreateOrder(ex, TypeName, context.Name);
            return;
        }

        await _orders
            .SetOrderAsync(created, command.StopPrice.GetValueOrDefault(0), 0m, command.Notional.GetValueOrDefault(0), cancellationToken)
            .ConfigureAwait(false);

        LogPlacedOrder(TypeName, command.Symbol.Name, command.Type, command.Side, command.Quantity, command.Notional, command.Symbol.BaseAsset, command.Price, command.Symbol.QuoteAsset, command.Notional.HasValue ? command.Notional : command.Quantity * command.Price, watch.ElapsedMilliseconds);
    }

    #region Logging

    [LoggerMessage(1, LogLevel.Information, "{Type} {Name} placing {OrderType} {OrderSide} order with (Quantity: {Quantity:F8} {Asset}, Notional: {Notional:F8} {Quote}, Price: {Price:F8} {Quote}, Total: {Total:F8} {Quote})")]
    private partial void LogPlacingOrder(string type, string name, OrderType orderType, OrderSide orderSide, decimal? quantity, decimal? notional, string asset, decimal? price, string quote, decimal? total);

    [LoggerMessage(2, LogLevel.Information, "{Type} {Name} placed {OrderType} {OrderSide} order with (Quantity: {Quantity:F8} {Asset}, Notional: {Notional:F8} {Quote}, Price: {Price:F8} {Quote}, Total: {Total:F8} {Quote}) in {ElapsedMs}ms")]
    private partial void LogPlacedOrder(string type, string name, OrderType orderType, OrderSide orderSide, decimal? quantity, decimal? notional, string asset, decimal? price, string quote, decimal? total, long elapsedMs);

    [LoggerMessage(3, LogLevel.Error, "{Type} {Name} cannot place {OrderType} {OrderSide} order requiring {Quantity:F8} {Asset} because there is only {Free:F8} {Asset} free")]
    private partial void LogCannotPlaceOrderWithoutFreeQuantity(string type, string name, OrderType orderType, OrderSide orderSide, decimal quantity, string asset, decimal free);

    [LoggerMessage(4, LogLevel.Error, "{Type} {Name} failed to create order")]
    private partial void LogFailedToCreateOrder(Exception ex, string type, string name);

    #endregion Logging
}