using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Context;

namespace Outcompute.Trader.Trading.Commands.EnsureSingleOrder;

public class EnsureSingleOrderCommand : IAlgoCommand
{
    public EnsureSingleOrderCommand(Symbol symbol, OrderSide side, OrderType type, TimeInForce? timeInForce, decimal? quantity, decimal? notional, decimal? price, decimal? stopPrice, string? tag = null)
    {
        Guard.IsNotNull(symbol, nameof(symbol));

        Symbol = symbol;
        Side = side;
        Type = type;
        TimeInForce = timeInForce;
        Quantity = quantity;
        Notional = notional;
        Price = price;
        StopPrice = stopPrice;
        Tag = tag;
    }

    public Symbol Symbol { get; }
    public OrderSide Side { get; }
    public OrderType Type { get; }
    public TimeInForce? TimeInForce { get; }
    public decimal? Quantity { get; }
    public decimal? Notional { get; }
    public decimal? Price { get; }
    public decimal? StopPrice { get; }
    public string? Tag { get; }

    public ValueTask ExecuteAsync(IAlgoContext context, CancellationToken cancellationToken = default)
    {
        return context.ServiceProvider
            .GetRequiredService<IAlgoCommandExecutor<EnsureSingleOrderCommand>>()
            .ExecuteAsync(context, this, cancellationToken);
    }
}