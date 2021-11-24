using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Context;

namespace Outcompute.Trader.Trading.Commands.CreateOrder;

internal class CreateOrderCommand : AlgoCommandBase
{
    /// <summary>
    /// Creates an order on the exchange.
    /// </summary>
    /// <param name="symbol">The symbol for the order.</param>
    /// <param name="type">The type of the order.</param>
    /// <param name="side">The side of the order.</param>
    /// <param name="timeInForce">How long the order should stay in place.</param>
    /// <param name="quantity">The quantity of the order.</param>
    /// <param name="notional">The notional of the order.</param>
    /// <param name="price">The price of the order.</param>
    /// <param name="stopPrice">The stop price of the order.</param>
    /// <param name="tag">The open order tag of the order.</param>
    internal CreateOrderCommand(Symbol symbol, OrderType type, OrderSide side, TimeInForce? timeInForce, decimal? quantity, decimal? notional, decimal? price, decimal? stopPrice, string? tag)
        : base(symbol)
    {
        switch (type)
        {
            case OrderType.Limit:
                if (timeInForce is null) ThrowHelper.ThrowArgumentNullException(nameof(timeInForce), $"'{nameof(OrderType)}' '{OrderType.Limit}' requires '{nameof(timeInForce)}'");
                if (quantity is null) ThrowHelper.ThrowArgumentNullException(nameof(quantity), $"'{nameof(OrderType)}' '{OrderType.Limit}' requires '{nameof(quantity)}'");
                if (price is null) ThrowHelper.ThrowArgumentNullException(nameof(price), $"'{nameof(OrderType)}' '{OrderType.Limit}' requires '{nameof(price)}'");
                break;

            case OrderType.Market:
                if (quantity is null && notional is null) ThrowHelper.ThrowArgumentException($"'{nameof(OrderType)}' '{OrderType.Market}' requires '{nameof(quantity)}' or '{nameof(notional)}'");
                if (quantity is not null && notional is not null) ThrowHelper.ThrowArgumentException($"'{nameof(OrderType)}' '{OrderType.Market}' allows only one of '{nameof(quantity)}' or '{nameof(notional)}'");
                break;

            case OrderType.StopLoss:
                if (quantity is null) ThrowHelper.ThrowArgumentNullException(nameof(quantity), $"'{nameof(OrderType)}' '{OrderType.StopLoss}' requires '{nameof(quantity)}'");
                if (stopPrice is null) ThrowHelper.ThrowArgumentNullException(nameof(stopPrice), $"'{nameof(OrderType)}' '{OrderType.StopLoss}' requires '{nameof(stopPrice)}'");
                break;

            case OrderType.StopLossLimit:
                if (timeInForce is null) ThrowHelper.ThrowArgumentNullException(nameof(timeInForce), $"'{nameof(OrderType)}' '{OrderType.StopLossLimit}' requires '{nameof(timeInForce)}'");
                if (quantity is null) ThrowHelper.ThrowArgumentNullException(nameof(quantity), $"'{nameof(OrderType)}' '{OrderType.StopLossLimit}' requires '{nameof(quantity)}'");
                if (price is null) ThrowHelper.ThrowArgumentNullException(nameof(price), $"'{nameof(OrderType)}' '{OrderType.StopLossLimit}' requires '{nameof(price)}'");
                if (stopPrice is null) ThrowHelper.ThrowArgumentNullException(nameof(stopPrice), $"'{nameof(OrderType)}' '{OrderType.StopLossLimit}' requires '{nameof(stopPrice)}'");
                break;

            case OrderType.TakeProfit:
                if (quantity is null) ThrowHelper.ThrowArgumentNullException(nameof(quantity), $"'{nameof(OrderType)}' '{OrderType.TakeProfit}' requires '{nameof(quantity)}'");
                if (stopPrice is null) ThrowHelper.ThrowArgumentNullException(nameof(stopPrice), $"'{nameof(OrderType)}' '{OrderType.TakeProfit}' requires '{nameof(stopPrice)}'");
                break;

            case OrderType.TakeProfitLimit:
                if (timeInForce is null) ThrowHelper.ThrowArgumentNullException(nameof(timeInForce), $"'{nameof(OrderType)}' '{OrderType.TakeProfitLimit}' requires '{nameof(timeInForce)}'");
                if (quantity is null) ThrowHelper.ThrowArgumentNullException(nameof(quantity), $"'{nameof(OrderType)}' '{OrderType.TakeProfitLimit}' requires '{nameof(quantity)}'");
                if (price is null) ThrowHelper.ThrowArgumentNullException(nameof(price), $"'{nameof(OrderType)}' '{OrderType.TakeProfitLimit}' requires '{nameof(price)}'");
                if (stopPrice is null) ThrowHelper.ThrowArgumentNullException(nameof(stopPrice), $"'{nameof(OrderType)}' '{OrderType.TakeProfitLimit}' requires '{nameof(stopPrice)}'");
                break;

            case OrderType.LimitMaker:
                if (quantity is null) ThrowHelper.ThrowArgumentNullException(nameof(quantity), $"'{nameof(OrderType)}' '{OrderType.LimitMaker}' requires '{nameof(quantity)}'");
                if (price is null) ThrowHelper.ThrowArgumentNullException(nameof(price), $"'{nameof(OrderType)}' '{OrderType.LimitMaker}' requires '{nameof(price)}'");
                break;

            default:
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(type));
                break;
        }

        Type = type;
        Side = side;
        TimeInForce = timeInForce;
        Quantity = quantity;
        Notional = notional;
        Price = price;
        StopPrice = stopPrice;
        Tag = tag;
    }

    public OrderType Type { get; }
    public OrderSide Side { get; }
    public TimeInForce? TimeInForce { get; }
    public decimal? Quantity { get; }
    public decimal? Notional { get; }
    public decimal? Price { get; }
    public decimal? StopPrice { get; }
    public string? Tag { get; }

    public override ValueTask ExecuteAsync(IAlgoContext context, CancellationToken cancellationToken = default)
    {
        return context.ServiceProvider
            .GetRequiredService<IAlgoCommandExecutor<CreateOrderCommand>>()
            .ExecuteAsync(context, this, cancellationToken);
    }
}