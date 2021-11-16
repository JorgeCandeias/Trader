using Microsoft.Extensions.DependencyInjection;

namespace Outcompute.Trader.Trading.Binance.Providers.UserData;

internal class BinanceUserDataStreamWssClientFactory : IUserDataStreamClientFactory
{
    private readonly IServiceProvider _provider;

    public BinanceUserDataStreamWssClientFactory(IServiceProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    public IUserDataStreamClient Create(string listenKey)
    {
        return ActivatorUtilities.CreateInstance<BinanceUserDataStreamWssClient>(_provider, listenKey);
    }
}