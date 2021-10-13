using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace Outcompute.Trader.Trading.Algorithms
{
    public static class IAlgoContextExtensions
    {
        public static ILogger GetLogger(this IAlgoContext context)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            return context.ServiceProvider.GetRequiredService<ILogger<IAlgoContext>>();
        }
    }
}