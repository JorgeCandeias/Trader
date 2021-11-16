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
        // populate orders for the default symbol
        if (!IsNullOrEmpty(context.Symbol.Name))
        {
            context.Orders = await _orders
                .GetOrdersAsync(context.Symbol.Name, CancellationToken.None)
                .ConfigureAwait(false);
        }

        // populate orders for the multi symbol list
        if (context.Symbols.Count > 0)
        {
            foreach (var symbol in context.Symbols.Keys)
            {
                context.Orders = await _orders
                    .GetOrdersAsync(symbol, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}