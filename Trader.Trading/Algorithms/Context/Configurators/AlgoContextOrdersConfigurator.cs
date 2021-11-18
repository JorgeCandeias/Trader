using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers;

namespace Outcompute.Trader.Trading.Algorithms.Context.Configurators;

internal class AlgoContextOrdersConfigurator : IAlgoContextConfigurator<AlgoContext>
{
    private readonly IOrderProvider _orders;

    public AlgoContextOrdersConfigurator(IOrderProvider orders)
    {
        _orders = orders;
    }

    public async ValueTask ConfigureAsync(AlgoContext context, string name, CancellationToken cancellationToken = default)
    {
        foreach (var symbol in context.Symbols)
        {
            await ApplyAsync(context, symbol, cancellationToken).ConfigureAwait(false);
        }

        if (!IsNullOrEmpty(context.Symbol.Name) && !context.Symbols.Contains(context.Symbol.Name))
        {
            await ApplyAsync(context, context.Symbol, cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask ApplyAsync(AlgoContext context, Symbol symbol, CancellationToken cancellationToken)
    {
        context.Data.GetOrAdd(symbol.Name).Orders = await _orders
            .GetOrdersAsync(symbol.Name, cancellationToken)
            .ConfigureAwait(false);
    }
}