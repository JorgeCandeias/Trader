using Outcompute.Trader.Trading.Providers;

namespace Outcompute.Trader.Trading.Algorithms.Context.Configurators;

internal class AlgoContextExchangeInfoConfigurator : IAlgoContextConfigurator<AlgoContext>
{
    private readonly IExchangeInfoProvider _provider;

    public AlgoContextExchangeInfoConfigurator(IExchangeInfoProvider provider)
    {
        _provider = provider;
    }

    public async ValueTask ConfigureAsync(AlgoContext context, string name, CancellationToken cancellationToken = default)
    {
        context.ExchangeInfo = await _provider
            .GetExchangeInfoAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}