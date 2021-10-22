using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Trading.Providers;
using System;

namespace Outcompute.Trader.Trading.Algorithms
{
    public static class TradeProviderAlgoContextExtensions
    {
        public static ITradeProvider GetTradeProvider(this IAlgoContext context)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            return context.ServiceProvider.GetRequiredService<ITradeProvider>();
        }
    }
}