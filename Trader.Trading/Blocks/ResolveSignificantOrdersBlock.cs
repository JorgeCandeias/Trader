using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public static class ResolveSignificantOrdersBlock
    {
        public static ValueTask<SignificantResult> ResolveSignificantOrdersAsync(this IAlgoContext context, Symbol symbol, CancellationToken cancellationToken = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            return ResolveSignificantOrdersInnerAsync(context, symbol, cancellationToken);
        }

        private static ValueTask<SignificantResult> ResolveSignificantOrdersInnerAsync(IAlgoContext context, Symbol symbol, CancellationToken cancellationToken)
        {
            var resolver = context.ServiceProvider.GetRequiredService<ISignificantOrderResolver>();

            return resolver.ResolveAsync(symbol, cancellationToken);
        }
    }
}