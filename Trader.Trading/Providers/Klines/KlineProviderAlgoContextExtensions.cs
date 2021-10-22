using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers;
using Outcompute.Trader.Trading.Providers.Orders;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public static class KlineProviderAlgoContextExtensions
    {
        public static IKlineProvider GetKlineProvider(this IAlgoContext context)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            return context.ServiceProvider.GetRequiredService<IKlineProvider>();
        }

        public static Task<IReadOnlyList<Kline>> GetKlinesAsync(this IAlgoContext context, string symbol, KlineInterval interval, DateTime start, DateTime end, CancellationToken cancellationToken = default)
        {
            return context.GetKlineProvider().GetKlinesAsync(symbol, interval, start, end, cancellationToken);
        }
    }
}