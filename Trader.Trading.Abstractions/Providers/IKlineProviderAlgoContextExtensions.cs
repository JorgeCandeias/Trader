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
        public static IKlineProvider GetKlineProvider(this IAlgoContext context)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            return context.ServiceProvider.GetRequiredService<IKlineProvider>();
        }

        public static ValueTask<IReadOnlyCollection<Kline>> GetKlinesAsync(this IAlgoContext context, string symbol, KlineInterval interval, DateTime start, DateTime end, CancellationToken cancellationToken = default)
        {
            return context.GetKlineProvider().GetKlinesAsync(symbol, interval, start, end, cancellationToken);
        }
    }
}