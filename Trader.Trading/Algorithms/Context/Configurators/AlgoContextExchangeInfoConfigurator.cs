using Outcompute.Trader.Trading.Providers;

namespace Outcompute.Trader.Trading.Algorithms.Context.Configurators;

internal class AlgoContextExchangeInfoConfigurator : IAlgoContextConfigurator<AlgoContext>
{
    private readonly IExchangeInfoProvider _provider;

    public AlgoContextExchangeInfoConfigurator(IExchangeInfoProvider provider)
    {
        _provider = provider;
    }

    public ValueTask ConfigureAsync(AlgoContext context, string name, CancellationToken cancellationToken = default)
    {
        context.ExchangeInfo = _provider.GetExchangeInfo();

        return ValueTask.CompletedTask;
    }
}