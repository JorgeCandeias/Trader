using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public static class IKlineProviderAlgoContextExtensions
    {
        /// <inheritdoc cref="IKlineProvider.GetKlinesAsync(string, KlineInterval, DateTime, DateTime, CancellationToken)"/>
        public static ValueTask<IReadOnlyList<Kline>> GetKlinesAsync(this IAlgoContext context, string symbol, KlineInterval interval, DateTime start, DateTime end, CancellationToken cancellationToken = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            var provider = context.ServiceProvider.GetRequiredService<IKlineProvider>();

            return provider.GetKlinesAsync(symbol, interval, start, end, cancellationToken);
        }
    }
}