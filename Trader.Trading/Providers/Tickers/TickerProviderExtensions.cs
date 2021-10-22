using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Tickers
{
    public static class TickerProviderExtensions
    {
        public static Task<MiniTicker> GetRequiredTickerAsync(this ITickerProvider provider, string symbol, CancellationToken cancellationToken = default)
        {
            if (provider is null) throw new ArgumentNullException(nameof(provider));

            return GetRequiredTickerCoreAsync(provider, symbol, cancellationToken);

            static async Task<MiniTicker> GetRequiredTickerCoreAsync(ITickerProvider provider, string symbol, CancellationToken cancellationToken = default)
            {
                return await provider.TryGetTickerAsync(symbol, cancellationToken).ConfigureAwait(false)
                    ?? throw new KeyNotFoundException($"Could not get ticker for symbol '{symbol}'");
            }
        }
    }
}