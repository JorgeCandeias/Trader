using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Trading.Providers;
using System;

namespace Outcompute.Trader.Trading.Algorithms
{
    public static class ITickerProviderAlgoContextExtensions
    {
        public static ITickerProvider GetTickerProvider(this IAlgoContext context)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            return context.ServiceProvider.GetRequiredService<ITickerProvider>();
        }
    }
}