using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Providers;

public static class ExchangeInfoProviderExtensions
{
    public static Symbol GetRequiredSymbol(this IExchangeInfoProvider provider, string symbol)
    {
        if (provider is null) throw new ArgumentNullException(nameof(provider));

        return provider.TryGetSymbol(symbol) ?? throw new KeyNotFoundException($"Could not get symbol information for '{symbol}'");
    }
}