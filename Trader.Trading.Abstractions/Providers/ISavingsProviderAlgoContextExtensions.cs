using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers
{
    public static class ISavingsProviderAlgoContextExtensions
    {
        private static ISavingsProvider GetProvider(this IAlgoContext context)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            return context.ServiceProvider.GetRequiredService<ISavingsProvider>();
        }

        public static Task<IReadOnlyCollection<FlexibleProductPosition>> GetFlexibleProductPositionAsync(this IAlgoContext context, string asset, CancellationToken cancellationToken = default)
        {
            return context.GetProvider().GetFlexibleProductPositionAsync(asset, cancellationToken);
        }

        public static Task<LeftDailyRedemptionQuotaOnFlexibleProduct?> GetLeftDailyRedemptionQuotaOnFlexibleProductAsync(this IAlgoContext context, string asset, string productId, FlexibleProductRedemptionType type, CancellationToken cancellationToken = default)
        {
            return context.GetProvider().TryGetLeftDailyRedemptionQuotaOnFlexibleProductAsync(asset, productId, type, cancellationToken);
        }

        public static Task RedeemFlexibleProductAsync(this IAlgoContext context, string asset, string productId, decimal amount, FlexibleProductRedemptionType type, CancellationToken cancellationToken = default)
        {
            return context.GetProvider().RedeemFlexibleProductAsync(asset, productId, amount, type, cancellationToken);
        }
    }
}