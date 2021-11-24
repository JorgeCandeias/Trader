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
        if (quantity is null && notional is null)
        {
            ThrowHelper.ThrowArgumentException($"Specify one of '{nameof(quantity)}' or '{nameof(notional)}' arguments");
        }

        if (quantity is not null && notional is not null)
        {
            ThrowHelper.ThrowArgumentException($"Specify only one of '{nameof(quantity)}' or '{nameof(notional)}' and not both");
        }

        if (quantity is not null && price is null)
        {
            ThrowHelper.ThrowArgumentNullException(nameof(price), $"Specify '{nameof(price)}' when specifying '{nameof(quantity)}'");
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