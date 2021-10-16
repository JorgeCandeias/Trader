using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers
{
    public static class ISymbolProviderExtensions
    {
        public static ValueTask<Symbol> GetRequiredSymbolAsync(this ISymbolProvider provider, string symbol, CancellationToken cancellationToken = default)
        {
            if (provider is null) throw new ArgumentNullException(nameof(provider));

            return GetRequiredSymbolInnerAsync(provider, symbol, cancellationToken);
        }

        private static async ValueTask<Symbol> GetRequiredSymbolInnerAsync(this ISymbolProvider provider, string symbol, CancellationToken cancellationToken = default)
        {
            return await provider.TryGetSymbolAsync(symbol, cancellationToken).ConfigureAwait(false)
                ?? throw new KeyNotFoundException($"Could not get symbol information for '{symbol}'");
        }
    }
}