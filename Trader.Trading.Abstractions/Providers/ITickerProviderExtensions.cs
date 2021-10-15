using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers
{
    public static class ITickerProviderExtensions
    {
        public static ValueTask<MiniTicker> GetRequiredTickerAsync(this ITickerProvider provider, string symbol, CancellationToken cancellationToken = default)
        {
            if (provider is null) throw new ArgumentNullException(nameof(provider));

            return provider.GetRequiredTickerInnerAsync(symbol, cancellationToken);
        }

        private static async ValueTask<MiniTicker> GetRequiredTickerInnerAsync(this ITickerProvider provider, string symbol, CancellationToken cancellationToken = default)
        {
            return await provider.TryGetTickerAsync(symbol, cancellationToken).ConfigureAwait(false)
                ?? throw new KeyNotFoundException($"Could not get ticker information for symbol '{symbol}'");
        }
    }
}