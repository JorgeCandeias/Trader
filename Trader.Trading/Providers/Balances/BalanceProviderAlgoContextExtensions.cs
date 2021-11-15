using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Providers;
using System;

namespace Outcompute.Trader.Trading.Algorithms
{
    public static class BalanceProviderAlgoContextExtensions
    {
        public static IBalanceProvider GetBalanceProvider(this IAlgoContext context)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            return context.ServiceProvider.GetRequiredService<IBalanceProvider>();
        }
    }
}