using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers
{
    public static class KlineProviderExtensions
    {
        public static Task<IReadOnlyList<Kline>> GetKlinesAsync(this IKlineProvider provider, string symbol, KlineInterval interval, DateTime start, DateTime end, CancellationToken cancellationToken)
        {
            if (provider is null) throw new ArgumentNullException(nameof(provider));
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            return provider.GetKlinesCoreAsync(symbol, interval, start, end, cancellationToken);
        }

        private static async Task<IReadOnlyList<Kline>> GetKlinesCoreAsync(this IKlineProvider provider, string symbol, KlineInterval interval, DateTime start, DateTime end, CancellationToken cancellationToken)
        {
            var result = await provider
                .GetKlinesAsync(symbol, interval, cancellationToken)
                .ConfigureAwait(false);

            return result.Where(x => x.OpenTime >= start && x.OpenTime <= end).ToImmutableList();
        }
    }
}