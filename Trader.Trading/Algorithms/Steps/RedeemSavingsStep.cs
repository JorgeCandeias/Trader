using Microsoft.Extensions.Logging;
using Outcompute.Trader.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms.Steps
{
    internal class RedeemSavingsStep : IRedeemSavingsStep
    {
        private readonly ILogger _logger;
        private readonly ITradingService _trader;

        public RedeemSavingsStep(ILogger<RedeemSavingsStep> logger, ITradingService trader)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _trader = trader ?? throw new ArgumentNullException(nameof(trader));
        }

        private static string Type => nameof(RedeemSavingsStep);

        public async Task<bool> GoAsync(string asset, decimal amount, CancellationToken cancellationToken = default)
        {
            // get the current savings for this asset
            var positions = await _trader
                .GetFlexibleProductPositionAsync(asset, cancellationToken)
                .ConfigureAwait(false);

            // stop if there are no savings
            if (positions.Count is 0)
            {
                _logger.LogError(
                    "{Type} cannot redeem the necessary amount of {Quantity} {Asset} because there are no savings for this asset",
                    Type, amount, asset);

                return false;
            }

            // there should only be one item in the result
            var savings = positions.Single();

            // check if we can redeem at all - we cant redeem during maintenance windows etc
            if (!savings.CanRedeem)
            {
                _logger.LogWarning("{Type} cannot redeem savings at this time because redeeming is disallowed", Type);

                return false;
            }

            // check if there is a redemption in progress
            if (savings.RedeemingAmount > 0)
            {
                _logger.LogWarning(
                    "{Type} will not redeem savings now because a redemption of {RedeemingAmount} {Asset} is in progress",
                    Type, savings.RedeemingAmount, asset);

                return false;
            }

            // check if there is enough for redemption
            if (savings.FreeAmount < amount)
            {
                _logger.LogError(
                    "{Type} cannot redeem the necessary {Quantity} {Asset} from savings because they only contain {FreeAmount} {Asset}",
                    Type, amount, asset, savings.FreeAmount, asset);

                return false;
            }

            var quota = await _trader
                .GetLeftDailyRedemptionQuotaOnFlexibleProductAsync(savings.ProductId, FlexibleProductRedemptionType.Fast, cancellationToken)
                .ConfigureAwait(false);

            // stop if there is no savings product
            if (quota is null)
            {
                _logger.LogError(
                    "{Type} cannot find a savings product for asset {Asset}",
                    Type, asset);

                return false;
            }

            // stop if we would exceed the daily quota outright
            if (quota.LeftQuota < amount)
            {
                _logger.LogError(
                    "{Type} cannot redeem the necessary amount of {Quantity} {Asset} because it exceeds the available quota of {Quota} {Asset}",
                    Type, amount, asset, quota.LeftQuota, asset);

                return false;
            }

            // bump the necessary value if needed now
            if (amount < quota.MinRedemptionAmount)
            {
                var bumped = Math.Min(quota.MinRedemptionAmount, savings.FreeAmount);

                _logger.LogInformation(
                    "{Type} bumped the necessary quantity of {Necessary} {Asset} to {Bumped} {Asset} to enable redemption",
                    Type, amount, asset, bumped, asset);

                amount = bumped;
            }

            // if we got here then we can attempt to redeem
            _logger.LogInformation(
                "{Type} attempting to redeem {Quantity} {Asset} from savings...",
                Type, amount, asset);

            await _trader
                .RedeemFlexibleProductAsync(savings.ProductId, amount, FlexibleProductRedemptionType.Fast, cancellationToken)
                .ConfigureAwait(false);

            // let the algo cycle to allow time for the redeemption to process
            return true;
        }
    }
}