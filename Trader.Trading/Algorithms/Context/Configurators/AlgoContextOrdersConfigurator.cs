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
        // populate orders for the multi symbol list
        foreach (var symbol in context.Symbols.Keys)
        {
            context.OrdersLookup[symbol] = await _orders
                .GetOrdersAsync(symbol, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}