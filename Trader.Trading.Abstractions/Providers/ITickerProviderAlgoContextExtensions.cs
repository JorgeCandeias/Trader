﻿using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public static class ITickerProviderAlgoContextExtensions
    {
        public static ITickerProvider GetTickerProvider(this IAlgoContext context)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            return context.ServiceProvider.GetRequiredService<ITickerProvider>();
        }

        public static ValueTask<MiniTicker?> TryGetTickerAsync(this IAlgoContext context, string symbol, CancellationToken cancellationToken = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            return context.ServiceProvider.GetRequiredService<ITickerProvider>().TryGetTickerAsync(symbol, cancellationToken);
        }

        public static ValueTask<MiniTicker> GetRequiredTickerAsync(this IAlgoContext context, string symbol, CancellationToken cancellationToken = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            return context.ServiceProvider.GetRequiredService<ITickerProvider>().GetRequiredTickerAsync(symbol, cancellationToken);
        }
    }
}