using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers
{
    public static class ExchangeInfoProviderExtensions
    {
        public static Task<Symbol> GetRequiredSymbolAsync(this IExchangeInfoProvider provider, string symbol, CancellationToken cancellationToken = default)
        {
            if (provider is null) throw new ArgumentNullException(nameof(provider));

            return GetRequiredSymbolCoreAsync(provider, symbol, cancellationToken);
        }

        private static async Task<Symbol> GetRequiredSymbolCoreAsync(this IExchangeInfoProvider provider, string symbol, CancellationToken cancellationToken = default)
        {
            return await provider.TryGetSymbolAsync(symbol, cancellationToken).ConfigureAwait(false)
                ?? throw new KeyNotFoundException($"Could not get symbol information for '{symbol}'");
        }
    }
}