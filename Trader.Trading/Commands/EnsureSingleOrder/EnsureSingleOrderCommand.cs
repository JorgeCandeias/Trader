using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Context;

namespace Outcompute.Trader.Trading.Commands.EnsureSingleOrder;

internal class EnsureSingleOrderCommand : AlgoCommandBase
{
    public EnsureSingleOrderCommand(Symbol symbol, OrderSide side, OrderType type, TimeInForce? timeInForce, decimal? quantity, decimal? notional, decimal? price, decimal? stopPrice, bool redeemSavings = false, bool redeemSwapPool = false)
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

        Side = side;
        Type = type;
        TimeInForce = timeInForce;
        Quantity = quantity;
        Notional = notional;
        Price = price;
        StopPrice = stopPrice;
        RedeemSavings = redeemSavings;
        RedeemSwapPool = redeemSwapPool;

        if (side != OrderSide.Buy && side != OrderSide.Sell) throw new ArgumentOutOfRangeException(nameof(side));
        if (price is null && stopPrice is null) throw new ArgumentException($"Provide one of '{nameof(price)}' or {nameof(stopPrice)}");
    }

    public OrderSide Side { get; }
    public OrderType Type { get; }
    public TimeInForce? TimeInForce { get; }
    public decimal? Quantity { get; }
    public decimal? Notional { get; }
    public decimal? Price { get; }
    public decimal? StopPrice { get; }
    public bool RedeemSavings { get; }
    public bool RedeemSwapPool { get; }

    public override ValueTask ExecuteAsync(IAlgoContext context, CancellationToken cancellationToken = default)
    {
        return context.ServiceProvider
            .GetRequiredService<IAlgoCommandExecutor<EnsureSingleOrderCommand>>()
            .ExecuteAsync(context, this, cancellationToken);
    }
}