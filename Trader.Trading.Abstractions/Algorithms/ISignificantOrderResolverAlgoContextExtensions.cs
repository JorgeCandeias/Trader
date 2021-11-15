using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Trading.Algorithms.Context;
using System;

namespace Outcompute.Trader.Trading.Algorithms
{
    public static class ISignificantOrderResolverAlgoContextExtensions
    {
        public static IAutoPositionResolver GetSignificantOrderResolver(this IAlgoContext context)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            return context.ServiceProvider.GetRequiredService<IAutoPositionResolver>();
        }
    }
}