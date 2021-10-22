using Microsoft.Extensions.Hosting;
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
        private readonly ISystemClock _clock;
        private readonly ITradingService _trader;
        private readonly IHostApplicationLifetime _lifetime;

        public SavingsGrain(IOptions<SavingsProviderOptions> options, ISystemClock clock, ITradingService trader, IHostApplicationLifetime lifetime)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _trader = trader ?? throw new ArgumentNullException(nameof(trader));
            _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
        }

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

        public async Task RedeemAsync(string productId, decimal amount, SavingsRedemptionType type)
        {
            await _trader.RedeemFlexibleProductAsync(productId, amount, type);

            Invalidate();
        }
    }
}