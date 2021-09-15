using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Blocks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public static class ITrackingBuyBlockAlgoContextExtensions
    {
        public static Task SetTrackingBuyAsync(this IAlgoContext context, Symbol symbol, decimal pullbackRatio, decimal targetQuoteBalanceFractionPerBuy, decimal? maxNotional, CancellationToken cancellationToken = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            var block = context.ServiceProvider.GetRequiredService<ITrackingBuyBlock>();

            return block.GoAsync(symbol, pullbackRatio, targetQuoteBalanceFractionPerBuy, maxNotional, cancellationToken);
        }
    }
}