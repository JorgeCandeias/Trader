using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Outcompute.Trader.Models;
using System;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public static class IExchangeInfoAlgoContextExtensions
    {
        public static ValueTask<ExchangeInfo> GetExchangeInfoAsync(this IAlgoContext context)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            return context.ServiceProvider
                .GetRequiredService<IGrainFactory>()
                .GetExchangeInfoReplicaGrain()
                .GetExchangeInfoAsync();
        }

        public static ValueTask<Symbol?> TryGetSymbolAsync(this IAlgoContext context, string name)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            return context.ServiceProvider
                .GetRequiredService<IGrainFactory>()
                .GetExchangeInfoReplicaGrain()
                .TryGetSymbolAsync(name);
        }
    }
}