using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Context;

namespace Outcompute.Trader.Trading.Commands.EnsureSingleOrder;

internal class EnsureSingleOrderCommand : AlgoCommandBase
{
    public EnsureSingleOrderCommand(Symbol symbol, OrderSide side, OrderType type, TimeInForce? timeInForce, decimal? quantity, decimal? notional, decimal? price, decimal? stopPrice, string? tag = null, bool redeemSavings = false, bool redeemSwapPool = false)
        : base(symbol)
    {
        Side = side;
        Type = type;
        TimeInForce = timeInForce;
        Quantity = quantity;
        Notional = notional;
        Price = price;
        StopPrice = stopPrice;
        Tag = tag;
        RedeemSavings = redeemSavings;
        RedeemSwapPool = redeemSwapPool;
    }

    public OrderSide Side { get; }
    public OrderType Type { get; }
    public TimeInForce? TimeInForce { get; }
    public decimal? Quantity { get; }
    public decimal? Notional { get; }
    public decimal? Price { get; }
    public decimal? StopPrice { get; }
    public string? Tag { get; }
    public bool RedeemSavings { get; }
    public bool RedeemSwapPool { get; }

    public override ValueTask ExecuteAsync(IAlgoContext context, CancellationToken cancellationToken = default)
    {
        return context.ServiceProvider
            .GetRequiredService<IAlgoCommandExecutor<EnsureSingleOrderCommand>>()
            .ExecuteAsync(context, this, cancellationToken);
    }
}