using Microsoft.Extensions.DependencyInjection;
using System;

namespace Outcompute.Trader.Trading.Algorithms
{
    public static class ISignificantOrderResolverAlgoContextExtensions
    {
        public static ISignificantOrderResolver GetSignificantOrderResolver(this IAlgoContext context)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            return context.ServiceProvider.GetRequiredService<ISignificantOrderResolver>();
        }
    }
}