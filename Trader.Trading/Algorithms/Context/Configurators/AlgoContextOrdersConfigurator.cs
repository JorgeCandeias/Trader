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
        if (IsNullOrWhiteSpace(context.Symbol.Name))
        {
            return;
        }

        context.Orders = await _orders
            .GetOrdersAsync(context.Symbol.Name, CancellationToken.None)
            .ConfigureAwait(false);
    }
}