using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public static class RedeemSavingsBlock
    {
        private static string TypeName => nameof(RedeemSavingsBlock);

        public static ValueTask<(bool Success, decimal Redeemed)> TryRedeemSavingsAsync(this IAlgoContext context, string asset, decimal amount, CancellationToken cancellationToken = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));
            if (asset is null) throw new ArgumentNullException(nameof(context));

            return TryRedeemSavingsInnerAsync(context, asset, amount, cancellationToken);
        }

        private static async ValueTask<(bool Success, decimal Redeemed)> TryRedeemSavingsInnerAsync(IAlgoContext context, string asset, decimal amount, CancellationToken cancellationToken)
        {
            var logger = context.ServiceProvider.GetRequiredService<ILogger<IAlgoContext>>();
            var savingsProvider = context.ServiceProvider.GetRequiredService<ISavingsProvider>();

            // get the current savings for this asset
            var savings = await savingsProvider.TryGetFirstFlexibleProductPositionAsync(asset, cancellationToken).ConfigureAwait(false)
                ?? FlexibleProductPosition.Zero(asset);

            // check if we can redeem at all - we cant redeem during maintenance windows etc
            if (!savings.CanRedeem)
            {
                logger.LogWarning("{Type} cannot redeem savings at this time because redeeming is disallowed", TypeName);

                return (false, 0m);
            }

            // check if there is a redemption in progress
            if (savings.RedeemingAmount > 0)
            {
                logger.LogWarning(
                    "{Type} will not redeem savings now because a redemption of {RedeemingAmount} {Asset} is in progress",
                    TypeName, savings.RedeemingAmount, asset);

                return (false, 0m);
            }

            // check if there is enough for redemption
            if (savings.FreeAmount < amount)
            {
                logger.LogError(
                    "{Type} cannot redeem the necessary {Quantity} {Asset} from savings because they only contain {FreeAmount} {Asset}",
                    TypeName, amount, asset, savings.FreeAmount, asset);

                return (false, 0m);
            }

            var quota = await savingsProvider
                .TryGetLeftDailyRedemptionQuotaOnFlexibleProductAsync(savings.Asset, savings.ProductId, FlexibleProductRedemptionType.Fast, cancellationToken)
                .ConfigureAwait(false);

            // stop if there is no savings product
            if (quota is null)
            {
                logger.LogError(
                    "{Type} cannot find a savings product for asset {Asset}",
                    TypeName, asset);

                return (false, 0m);
            }

            // stop if we would exceed the daily quota outright
            if (quota.LeftQuota < amount)
            {
                logger.LogError(
                    "{Type} cannot redeem the necessary amount of {Quantity} {Asset} because it exceeds the available quota of {Quota} {Asset}",
                    TypeName, amount, asset, quota.LeftQuota, asset);

                return (false, 0m);
            }

            // bump the necessary value if needed now
            if (amount < quota.MinRedemptionAmount)
            {
                var bumped = Math.Min(quota.MinRedemptionAmount, savings.FreeAmount);

                logger.LogInformation(
                    "{Type} bumped the necessary quantity of {Necessary} {Asset} to {Bumped} {Asset} to enable redemption",
                    TypeName, amount, asset, bumped, asset);

                amount = bumped;
            }

            // if we got here then we can attempt to redeem
            logger.LogInformation(
                "{Type} attempting to redeem {Quantity} {Asset} from savings...",
                TypeName, amount, asset);

            await savingsProvider
                .RedeemFlexibleProductAsync(savings.Asset, savings.ProductId, amount, FlexibleProductRedemptionType.Fast, cancellationToken)
                .ConfigureAwait(false);

            // let the algo cycle to allow time for the redeemption to process
            return (true, amount);
        }
    }
}