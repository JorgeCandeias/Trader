using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Binance.Providers.Savings
{
    internal class BinanceSavingsGrain : Grain, IBinanceSavingsGrain
    {
        private readonly BinanceOptions _options;
        private readonly ILogger _logger;
        private readonly ISystemClock _clock;
        private readonly ITradingService _trader;

        public BinanceSavingsGrain(IOptions<BinanceOptions> options, ILogger<BinanceSavingsGrain> logger, ISystemClock clock, ITradingService trader)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _trader = trader ?? throw new ArgumentNullException(nameof(trader));
        }

        private string _asset = string.Empty;

        private readonly CancellationTokenSource _cancellation = new();
        private readonly FlexibleProductRedemptionType[] _redemptionTypes = new[] { FlexibleProductRedemptionType.Fast };

        #region Cache

        private ImmutableList<FlexibleProductPosition> _positions = ImmutableList<FlexibleProductPosition>.Empty;
        private readonly Dictionary<(string ProductId, FlexibleProductRedemptionType Type), LeftDailyRedemptionQuotaOnFlexibleProduct> _quotas = new();

        private DateTime _expiration = DateTime.MinValue;

        #endregion Cache

        public override async Task OnActivateAsync()
        {
            _asset = this.GetPrimaryKeyString();

            await base.OnActivateAsync();
        }

        public override Task OnDeactivateAsync()
        {
            _cancellation.Cancel();

            return base.OnDeactivateAsync();
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
            _positions = ImmutableList<FlexibleProductPosition>.Empty;
            _quotas.Clear();
        }

        private async Task UpdateAsync()
        {
            var result = await _trader
                .WithBackoff()
                .GetFlexibleProductPositionsAsync(_asset, _cancellation.Token);

            _positions = result.ToImmutableList();

            foreach (var productId in _positions.Select(x => x.ProductId))
            {
                foreach (var type in _redemptionTypes)
                {
                    var quota = await _trader
                        .WithBackoff()
                        .TryGetLeftDailyRedemptionQuotaOnFlexibleProductAsync(productId, type, _cancellation.Token);

                    if (quota is not null)
                    {
                        _quotas[(productId, type)] = quota;
                    }
                }
            }
        }

        public async ValueTask<IReadOnlyCollection<FlexibleProductPosition>> GetFlexibleProductPositionsAsync()
        {
            await EnsureUpdatedAsync();

            return _positions;
        }

        public async ValueTask<FlexibleProductPosition?> TryGetFirstFlexibleProductPositionAsync()
        {
            await EnsureUpdatedAsync();

            return _positions.Count > 0 ? _positions[0] : null;
        }

        public async ValueTask<LeftDailyRedemptionQuotaOnFlexibleProduct?> TryGetLeftDailyRedemptionQuotaOnFlexibleProductAsync(string productId, FlexibleProductRedemptionType type)
        {
            await EnsureUpdatedAsync();

            return _quotas.TryGetValue((productId, type), out var value) ? value : null;
        }

        public async ValueTask RedeemFlexibleProductAsync(string productId, decimal amount, FlexibleProductRedemptionType type)
        {
            await _trader.RedeemFlexibleProductAsync(productId, amount, type);

            Invalidate();
        }
    }
}