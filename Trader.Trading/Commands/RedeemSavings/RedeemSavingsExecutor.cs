using Microsoft.Extensions.Logging;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Providers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Commands.RedeemSavings
{
    internal class RedeemSavingsExecutor : IAlgoCommandExecutor<RedeemSavingsCommand, RedeemSavingsEvent>
    {
        private readonly ILogger _logger;
        private readonly ISavingsProvider _savings;

        public RedeemSavingsExecutor(ILogger<RedeemSavingsExecutor> logger, ISavingsProvider savings)
        {
            _logger = logger;
            _savings = savings;
        }

        private static string TypeName => nameof(RedeemSavingsExecutor);

        public async Task<RedeemSavingsEvent> ExecuteAsync(IAlgoContext context, RedeemSavingsCommand command, CancellationToken cancellationToken = default)
        {
            // get the current savings for this asset
            var savings = await _savings.GetPositionOrZeroAsync(command.Asset, cancellationToken).ConfigureAwait(false);

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
                    TypeName, savings.RedeemingAmount, command.Asset);

                return new RedeemSavingsEvent(false, 0m);
            }

            // check if there is enough for redemption
            if (savings.FreeAmount < command.Amount)
            {
                _logger.LogError(
                    "{Type} cannot redeem the necessary {Quantity} {Asset} from savings because they only contain {FreeAmount} {Asset}",
                    TypeName, command.Amount, command.Asset, savings.FreeAmount, command.Asset);

                return new RedeemSavingsEvent(false, 0m);
            }

            var quota = await _savings
                .GetQuotaOrZeroAsync(savings.Asset, savings.ProductId, SavingsRedemptionType.Fast, cancellationToken)
                .ConfigureAwait(false);

            // stop if we would exceed the daily quota outright
            if (quota.LeftQuota < command.Amount)
            {
                _logger.LogError(
                    "{Type} cannot redeem the necessary amount of {Quantity} {Asset} because it exceeds the available quota of {Quota} {Asset}",
                    TypeName, command.Amount, command.Asset, quota.LeftQuota, command.Asset);

                return new RedeemSavingsEvent(false, 0m);
            }

            // bump the necessary value if needed now
            var amount = command.Amount;
            if (command.Amount < quota.MinRedemptionAmount)
            {
                var bumped = Math.Min(quota.MinRedemptionAmount, savings.FreeAmount);

                _logger.LogInformation(
                    "{Type} bumped the necessary quantity of {Necessary} {Asset} to {Bumped} {Asset} to enable redemption",
                    TypeName, command.Amount, command.Asset, bumped, command.Asset);

                amount = bumped;
            }

            // if we got here then we can attempt to redeem
            _logger.LogInformation(
                "{Type} attempting to redeem {Quantity} {Asset} from savings...",
                TypeName, amount, command.Asset);

            await _savings
                .RedeemAsync(savings.Asset, savings.ProductId, amount, SavingsRedemptionType.Fast, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "{Type} redeemed {Quantity} {Asset} from savings",
                TypeName, amount, command.Asset);

            // let the algo cycle to allow time for the redeemption to process
            return new RedeemSavingsEvent(true, amount);
        }
    }
}