using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Context;

namespace Outcompute.Trader.Trading.Commands.EnsureSingleOrder;

public class EnsureSingleOrderCommand : IAlgoCommand
{
    public EnsureSingleOrderCommand(Symbol symbol, OrderSide side, OrderType type, TimeInForce? timeInForce, decimal quantity, decimal? price, decimal? stopPrice, bool redeemSavings = false, bool redeemSwapPool = false)
    {
        Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
        Side = side;
        Type = type;
        TimeInForce = timeInForce;
        Quantity = quantity;
        Price = price;
        StopPrice = stopPrice;
        RedeemSavings = redeemSavings;
        RedeemSwapPool = redeemSwapPool;

        if (side != OrderSide.Buy && side != OrderSide.Sell) throw new ArgumentOutOfRangeException(nameof(side));
        if (price is null && stopPrice is null) throw new ArgumentException($"Provide one of '{nameof(price)}' or {nameof(stopPrice)}");
    }

    public Symbol Symbol { get; }
    public OrderSide Side { get; }
    public OrderType Type { get; }
    public TimeInForce? TimeInForce { get; }
    public decimal Quantity { get; }
    public decimal? Price { get; }
    public decimal? StopPrice { get; }
    public bool RedeemSavings { get; }
    public bool RedeemSwapPool { get; }

    public ValueTask ExecuteAsync(IAlgoContext context, CancellationToken cancellationToken = default)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        return context.ServiceProvider
            .GetRequiredService<IAlgoCommandExecutor<EnsureSingleOrderCommand>>()
            .ExecuteAsync(context, this, cancellationToken);
    }
}