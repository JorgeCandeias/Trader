using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Trader.Models;

namespace Trader.Trading.Algorithms.Steps
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

        public async Task<bool> GoAsync(Symbol symbol, decimal amount, CancellationToken cancellationToken = default)
        {
            // see if there is a savings product at all for the asset
            var products = _trader.GetCachedFlexibleProductsByAsset(symbol.BaseAsset);

            // stop if there are no products for this asset
            if (products.Count is 0)
            {
                _logger.LogError(
                    "{Type} {Name} cannot redeem the necessary {Amount} because there are no flexible products for {Asset}",
                    Type, symbol.Name, amount, symbol.BaseAsset);

                return false;
            }

            // for now we assume there is a single product
            var product = products.Single();

            var quota = await _trader
                .GetLeftDailyRedemptionQuotaOnFlexibleProductAsync(product.ProductId, FlexibleProductRedemptionType.Fast, cancellationToken)
                .ConfigureAwait(false);

            // stop if there is no savings product
            if (quota is null)
            {
                _logger.LogError(
                    "{Type} {Name} cannot find a savings product for asset {Asset}",
                    Type, symbol.Name, symbol.BaseAsset);

                return false;
            }

            // stop if we would exceed the daily quota outright
            if (quota.LeftQuota < amount)
            {
                _logger.LogError(
                    "{Type} {Name} cannot redeem the necessary amount of {Quantity} {Asset} because it exceeds the available quota of {Quota} {Asset}",
                    Type, symbol.Name, amount, symbol.BaseAsset, quota.LeftQuota, symbol.BaseAsset);

                return false;
            }

            // get the current savings for this asset
            var positions = await _trader
                .GetFlexibleProductPositionAsync(symbol.BaseAsset, cancellationToken)
                .ConfigureAwait(false);

            // stop if there are no savings
            if (positions.Count is 0)
            {
                _logger.LogError(
                    "{Type} {Name} cannot redeem the necessary amount of {Quantity} {Asset} because there are no savings for {Asset}",
                    Type, symbol.Name, amount, symbol.BaseAsset, symbol.BaseAsset);

                return false;
            }

            // there should only be one item in the result
            var savings = positions.Single();

            // check if we can redeem at all - we cant redeem during maintenance windows etc
            if (!savings.CanRedeem)
            {
                _logger.LogWarning(
                    "{Type} {Name} cannot redeem savings at this time because redeeming is disallowed",
                    Type, symbol.Name);

                return false;
            }

            // check if there is a redemption in progress
            if (savings.RedeemingAmount > 0)
            {
                _logger.LogWarning(
                    "{Type} {Name} will not redeem savings now because a redemption of {RedeemingAmount} {Asset} is in progress",
                    Type, symbol.Name, savings.RedeemingAmount, symbol.BaseAsset);

                return false;
            }

            // check if there is enough for redemption
            if (savings.FreeAmount < amount)
            {
                _logger.LogError(
                    "{Type} {Name} cannot redeem the necessary {Quantity} {Asset} from savings because they only contain {FreeAmount} {Asset}",
                    Type, symbol.Name, amount, symbol.BaseAsset, savings.FreeAmount, symbol.BaseAsset);

                return false;
            }

            // bump the necessary value if needed now
            if (amount < quota.MinRedemptionAmount)
            {
                var bumped = Math.Min(quota.MinRedemptionAmount, savings.FreeAmount);

                _logger.LogInformation(
                    "{Type} {Name} bumped the necessary quantity of {Necessary} {Asset} to {Bumped} {Asset} to enable redemption",
                    Type, symbol.Name, amount, symbol.BaseAsset, bumped, symbol.BaseAsset);

                amount = bumped;
            }

            // if we got here then we can attempt to redeem
            _logger.LogInformation(
                "{Type} {Name} attempting to redeem {Quantity} {Asset} from savings...",
                Type, symbol.Name, amount, symbol.BaseAsset);

            await _trader
                .RedeemFlexibleProductAsync(product.ProductId, amount, FlexibleProductRedemptionType.Fast, cancellationToken)
                .ConfigureAwait(false);

            // let the algo cycle to allow time for the redeemption to process
            return true;
        }
    }
}