using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Trading.Algorithms.Context;
using System;

namespace Outcompute.Trader.Trading.Providers
{
    public static class ISavingsProviderAlgoContextExtensions
    {
        public static ISavingsProvider GetSavingsProvider(this IAlgoContext context)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            return context.ServiceProvider.GetRequiredService<ISavingsProvider>();
        }
    }
}