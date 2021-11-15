using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Outcompute.Trader.Trading.Algorithms.Context;
using System;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public static class IProfitAggregatorAlgoContextExtensions
    {
        public static Task PublishProfitAsync(this IAlgoContext context, Profit profit)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));
            if (profit is null) throw new ArgumentNullException(nameof(profit));

            return context.ServiceProvider
                .GetRequiredService<IGrainFactory>()
                .GetProfitAggregatorLocalGrain()
                .PublishAsync(profit);
        }
    }
}