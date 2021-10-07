using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Trading.Algorithms;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers
{
    public static class IOrderProviderAlgoContextExtensions
    {
        public static Task<bool> IsReadyAsync(this IAlgoContext context, CancellationToken cancellationToken = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            var provider = context.ServiceProvider.GetRequiredService<IOrderProvider>();

            return provider.IsReadyAsync(cancellationToken);
        }
    }
}