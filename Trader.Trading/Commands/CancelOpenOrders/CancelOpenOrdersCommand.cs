using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Context;

namespace Outcompute.Trader.Trading.Commands.CancelOpenOrders;

internal class CancelOpenOrdersCommand : IAlgoCommand
{
    public CancelOpenOrdersCommand(Symbol symbol, OrderSide? side = null, decimal? distance = null, string? tag = null)
    {
        Guard.IsNotNull(symbol, nameof(symbol));

        if (distance.HasValue)
        {
            Guard.IsGreaterThanOrEqualTo(distance.Value, 0M, nameof(distance));
        }

        Symbol = symbol;
        Side = side;
        Distance = distance;
        Tag = tag;
    }

    public Symbol Symbol { get; }
    public OrderSide? Side { get; }
    public decimal? Distance { get; }
    public string? Tag { get; }

    public ValueTask ExecuteAsync(IAlgoContext context, CancellationToken cancellationToken = default)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        return context.ServiceProvider
            .GetRequiredService<IAlgoCommandExecutor<CancelOpenOrdersCommand>>()
            .ExecuteAsync(context, this, cancellationToken);
    }
}