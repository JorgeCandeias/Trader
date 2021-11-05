using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Savings
{
    internal class SavingsGrain : Grain, ISavingsGrain
    {
        private readonly SavingsProviderOptions _options;
        private readonly ILogger _logger;
        private readonly ISystemClock _clock;
        private readonly ITradingService _trader;
        private readonly IHostApplicationLifetime _lifetime;

        public SavingsGrain(IOptions<SavingsProviderOptions> options, ILogger<SavingsGrain> logger, ISystemClock clock, ITradingService trader, IHostApplicationLifetime lifetime)
        {
            _options = options.Value;
            _logger = logger;
            _clock = clock;
            _trader = trader;
            _lifetime = lifetime;
        }

        private static string TypeName => nameof(SavingsGrain);

        private string _asset = string.Empty;

        private readonly SavingsRedemptionType[] _redemptionTypes = new[] { SavingsRedemptionType.Fast };

        #region Cache

        private ImmutableList<SavingsPosition> _positions = ImmutableList<SavingsPosition>.Empty;
        private readonly Dictionary<(string ProductId, SavingsRedemptionType Type), SavingsQuota> _quotas = new();

        private DateTime _expiration = DateTime.MinValue;

        #endregion Cache

        public override async Task OnActivateAsync()
        {
            _asset = this.GetPrimaryKeyString();

            await base.OnActivateAsync();
        }

        private async ValueTask EnsureUpdatedAsync()
        {
            if (_clock.UtcNow < _expiration) return;

            await UpdateAsync();

            _expiration = _clock.UtcNow.Add(_options.SavingsCacheWindow);
        }

        private void Invalidate()
        {
            _expiration = DateTime.MinValue;
            _positions = ImmutableList<SavingsPosition>.Empty;
            _quotas.Clear();
        }

        private async Task UpdateAsync()
        {
            var result = await _trader
                .WithBackoff()
                .GetFlexibleProductPositionsAsync(_asset, _lifetime.ApplicationStopping);

            _positions = result.ToImmutableList();

            foreach (var productId in _positions.Select(x => x.ProductId))
            {
                foreach (var type in _redemptionTypes)
                {
                    var quota = await _trader
                        .WithBackoff()
                        .TryGetLeftDailyRedemptionQuotaOnFlexibleProductAsync(productId, type, _lifetime.ApplicationStopping);

                    if (quota is not null)
                    {
                        _quotas[(productId, type)] = quota;
                    }
                }
            }
        }

        public async Task<SavingsPosition?> TryGetPositionAsync()
        {
            await EnsureUpdatedAsync();

            return _positions.Count > 0 ? _positions[0] : null;
        }

        public async Task<SavingsQuota?> TryGetQuotaAsync(string productId, SavingsRedemptionType type)
        {
            await EnsureUpdatedAsync();

            return _quotas.TryGetValue((productId, type), out var value) ? value : null;
        }

        public async Task<RedeemSavingsEvent> RedeemAsync(decimal amount, SavingsRedemptionType type)
        {
            // get the current savings for this asset
            var savings = _positions.SingleOrDefault();
            if (savings is null)
            {
                _logger.LogWarning(
                    "{Type} cannot redeem savings for asset {Asset} because there is no savings product",
                    TypeName, _asset);

                return new RedeemSavingsEvent(false, 0m);
            }

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
                    TypeName, savings.RedeemingAmount, _asset);

                return new RedeemSavingsEvent(false, 0m);
            }

            // check if there is enough for redemption
            if (savings.FreeAmount < amount)
            {
                _logger.LogError(
                    "{Type} cannot redeem the necessary {Quantity} {Asset} from savings because they only contain {FreeAmount} {Asset}",
                    TypeName, amount, _asset, savings.FreeAmount, _asset);

                return new RedeemSavingsEvent(false, 0m);
            }

            var quota = _quotas.TryGetValue((savings.ProductId, SavingsRedemptionType.Fast), out var value) ? value : SavingsQuota.Empty;

            // stop if we would exceed the daily quota outright
            if (quota.LeftQuota < amount)
            {
                _logger.LogError(
                    "{Type} cannot redeem the necessary amount of {Quantity} {Asset} because it exceeds the available quota of {Quota} {Asset}",
                    TypeName, amount, _asset, quota.LeftQuota, _asset);

                return new RedeemSavingsEvent(false, 0m);
            }

            // bump the necessary value if needed now
            if (amount < quota.MinRedemptionAmount)
            {
                var bumped = Math.Min(quota.MinRedemptionAmount, savings.FreeAmount);

                _logger.LogInformation(
                    "{Type} bumped the necessary quantity of {Necessary} {Asset} to {Bumped} {Asset} to enable redemption",
                    TypeName, amount, _asset, bumped, _asset);

                amount = bumped;
            }

            // if we got here then we can attempt to redeem
            _logger.LogInformation(
                "{Type} attempting to redeem {Quantity} {Asset} from savings...",
                TypeName, amount, _asset);

            await _trader.RedeemFlexibleProductAsync(savings.ProductId, amount, type);

            _logger.LogInformation(
                "{Type} redeemed {Quantity} {Asset} from savings",
                TypeName, amount, _asset);

            Invalidate();

            return new RedeemSavingsEvent(true, amount);
        }
    }
}