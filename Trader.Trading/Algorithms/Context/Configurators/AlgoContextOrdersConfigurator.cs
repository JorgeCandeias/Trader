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
            context.Data.GetOrAdd(symbol.Name).Orders = await _orders
                .GetOrdersAsync(symbol.Name, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}