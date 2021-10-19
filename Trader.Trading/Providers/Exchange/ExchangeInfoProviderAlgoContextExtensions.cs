using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public static class ExchangeInfoProviderAlgoContextExtensions
    {
        public static IExchangeInfoProvider GetExchangeInfoProvider(this IAlgoContext context)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            return context.ServiceProvider.GetRequiredService<IExchangeInfoProvider>();
        }

        public static async Task<Symbol> GetRequiredSymbolAsync(this IAlgoContext context, string symbol, CancellationToken cancellationToken = default)
        {
            return await context.GetExchangeInfoProvider().TryGetSymbolAsync(symbol, cancellationToken).ConfigureAwait(false)
                ?? throw new KeyNotFoundException($"Could not get symbol information for '{symbol}'");
        }
    }
}