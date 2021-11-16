using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Context;

namespace Outcompute.Trader.Trading.Commands.CancelOpenOrders;

public class CancelOpenOrdersCommand : IAlgoCommand
{
    public CancelOpenOrdersCommand(Symbol symbol, OrderSide? side = null, decimal? distance = null)
    {
        Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
        Side = side;
        Distance = distance;

        if (distance.HasValue && distance.Value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(distance));
        }
    }

    public Symbol Symbol { get; }
    public OrderSide? Side { get; }
    public decimal? Distance { get; }

    public ValueTask ExecuteAsync(IAlgoContext context, CancellationToken cancellationToken = default)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        return context.ServiceProvider
            .GetRequiredService<IAlgoCommandExecutor<CancelOpenOrdersCommand>>()
            .ExecuteAsync(context, this, cancellationToken);
    }
}