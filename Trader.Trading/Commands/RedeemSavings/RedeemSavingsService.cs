using Microsoft.Extensions.Logging;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Commands;
using Outcompute.Trader.Trading.Providers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Commands.RedeemSavings
{
    internal class RedeemSavingsService : IRedeemSavingsService
    {
        private readonly ILogger _logger;
        private readonly ISavingsProvider _savings;

        public RedeemSavingsService(ILogger<RedeemSavingsService> logger, ISavingsProvider savings)
        {
            _logger = logger;
            _savings = savings;
        }

        private static string TypeName => nameof(RedeemSavingsService);

        public Task<RedeemSavingsEvent> TryRedeemSavingsAsync(string asset, decimal amount, CancellationToken cancellationToken = default)
        {
            if (asset is null) throw new ArgumentNullException(nameof(asset));

            return TryRedeemSavingsCoreAsync(asset, amount, cancellationToken);
        }

        private async Task<RedeemSavingsEvent> TryRedeemSavingsCoreAsync(string asset, decimal amount, CancellationToken cancellationToken)
        {
            // get the current savings for this asset
            var savings = await _savings.GetPositionOrZeroAsync(asset, cancellationToken).ConfigureAwait(false);

            // check if we can redeem at all - we cant redeem during maintenance windows etc
            if (!savings.CanRedeem)
            {
                _logger.LogWarning(
                    "{Type} cannot redeem savings at this time because redeeming is disallowed",
                    TypeName);

                return new RedeemSavingsEvent(false, 0m);
            }

            // check if there is a redemption in progress
            if (savings.RedeemingAmount > 0)
            {
                _logger.LogWarning(
                    "{Type} will not redeem savings now because a redemption of {RedeemingAmount} {Asset} is in progress",
                    TypeName, savings.RedeemingAmount, asset);

                return new RedeemSavingsEvent(false, 0m);
            }

            // check if there is enough for redemption
            if (savings.FreeAmount < amount)
            {
                _logger.LogError(
                    "{Type} cannot redeem the necessary {Quantity} {Asset} from savings because they only contain {FreeAmount} {Asset}",
                    TypeName, amount, asset, savings.FreeAmount, asset);

                return new RedeemSavingsEvent(false, 0m);
            }

            var quota = await _savings
                .TryGetQuotaAsync(savings.Asset, savings.ProductId, SavingsRedemptionType.Fast, cancellationToken)
                .ConfigureAwait(false);

            // stop if there is no savings product
            if (quota is null)
            {
                _logger.LogError(
                    "{Type} cannot find a savings product for asset {Asset}",
                    TypeName, asset);

                return new RedeemSavingsEvent(false, 0m);
            }

            // stop if we would exceed the daily quota outright
            if (quota.LeftQuota < amount)
            {
                _logger.LogError(
                    "{Type} cannot redeem the necessary amount of {Quantity} {Asset} because it exceeds the available quota of {Quota} {Asset}",
                    TypeName, amount, asset, quota.LeftQuota, asset);

                return new RedeemSavingsEvent(false, 0m);
            }

            // bump the necessary value if needed now
            if (amount < quota.MinRedemptionAmount)
            {
                var bumped = Math.Min(quota.MinRedemptionAmount, savings.FreeAmount);

                _logger.LogInformation(
                    "{Type} bumped the necessary quantity of {Necessary} {Asset} to {Bumped} {Asset} to enable redemption",
                    TypeName, amount, asset, bumped, asset);

                amount = bumped;
            }

            // if we got here then we can attempt to redeem
            _logger.LogInformation(
                "{Type} attempting to redeem {Quantity} {Asset} from savings...",
                TypeName, amount, asset);

            await _savings
                .RedeemAsync(savings.Asset, savings.ProductId, amount, SavingsRedemptionType.Fast, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "{Type} redeemed {Quantity} {Asset} from savings",
                TypeName, amount, asset);

            // let the algo cycle to allow time for the redeemption to process
            return new RedeemSavingsEvent(true, amount);
        }
    }
}