using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Providers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public static class OrderProviderAlgoContextExtensions
    {
        public static IOrderProvider GetOrderProvider(this IAlgoContext context)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            return context.ServiceProvider.GetRequiredService<IOrderProvider>();
        }

        public static Task<IReadOnlyList<OrderQueryResult>> GetOrdersAsync(this IAlgoContext context, string symbol, CancellationToken cancellationToken = default)
        {
            return context.GetOrderProvider().GetOrdersAsync(symbol, cancellationToken);
        }
    }
}