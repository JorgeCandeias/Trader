using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Context;

namespace Outcompute.Trader.Trading.Commands.MarketSell;

public class MarketSellCommand : IAlgoCommand
{
    public MarketSellCommand(Symbol symbol, decimal quantity, string? tag = null)
    {
        Guard.IsNotNull(symbol, nameof(symbol));

        Symbol = symbol;
        Quantity = quantity;
        Tag = tag;
    }

    public Symbol Symbol { get; }
    public decimal Quantity { get; }
    public string? Tag { get; }

    public ValueTask ExecuteAsync(IAlgoContext context, CancellationToken cancellationToken = default)
    {
        return context.ServiceProvider
            .GetRequiredService<IAlgoCommandExecutor<MarketSellCommand>>()
            .ExecuteAsync(context, this, cancellationToken);
    }
}