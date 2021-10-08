using Microsoft.Extensions.Logging;
using Orleans;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Models;
using Polly;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Binance.Providers.Savings
{
    internal class BinanceSavingsGrain : Grain, IBinanceSavingsGrain
    {
        private readonly ILogger _logger;
        private readonly ISystemClock _clock;
        private readonly ITradingService _trader;

        public BinanceSavingsGrain(ILogger<BinanceSavingsGrain> logger, ISystemClock clock, ITradingService trader)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _trader = trader ?? throw new ArgumentNullException(nameof(trader));
        }

        private static string Name => nameof(BinanceSavingsGrain);

        private string _asset = string.Empty;
        private ImmutableList<FlexibleProductPosition> _positions = ImmutableList<FlexibleProductPosition>.Empty;

        private readonly CancellationTokenSource _cancellation = new();
        private readonly Dictionary<(string ProductId, FlexibleProductRedemptionType Type), (LeftDailyRedemptionQuotaOnFlexibleProduct Quota, DateTime Timestamp)> _quotas = new();
        private readonly FlexibleProductRedemptionType[] _redemptionTypes = new[] { FlexibleProductRedemptionType.Fast };

        private bool _updated;

        public override async Task OnActivateAsync()
        {
            _asset = this.GetPrimaryKeyString();

            await TickUpdateAsync();

            // keep polling the api as necessary
            RegisterTimer(_ => TickUpdateAsync(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

            // force a poll every hour to account for auto-savings
            RegisterTimer(_ => TickInvalidateAsync(), null, TimeSpan.FromHours(1), TimeSpan.FromHours(1));

            await base.OnActivateAsync();
        }

        public override Task OnDeactivateAsync()
        {
            _cancellation.Dispose();

            return base.OnDeactivateAsync();
        }

        private async Task TickUpdateAsync()
        {
            if (_updated) return;

            var result = await Policy
                .Handle<BinanceTooManyRequestsException>()
                .WaitAndRetryForeverAsync(
                    (n, ex, ctx) => ((BinanceTooManyRequestsException)ex).RetryAfter,
                    (ex, ts, ctx) => { _logger.LogWarning(ex, "{Name} backing off for {TimeSpan}...", Name, ts); return Task.CompletedTask; })
                .ExecuteAsync(ct => _trader.GetFlexibleProductPositionAsync(_asset), _cancellation.Token, true);

            _positions = result.ToImmutableList();

            _logger.LogInformation("{Name} {Asset} cached {Count} product positions", Name, _asset, _positions.Count);

            var redeeming = false;

            foreach (var position in _positions)
            {
                if (position.RedeemingAmount > 0)
                {
                    redeeming = true;
                }

                foreach (var type in _redemptionTypes)
                {
                    var quota = await Policy
                        .Handle<BinanceTooManyRequestsException>()
                        .WaitAndRetryForeverAsync(
                            (n, ex, ctx) => ((BinanceTooManyRequestsException)ex).RetryAfter,
                            (ex, ts, ctx) => { _logger.LogWarning(ex, "{Name} backing off for {TimeSpan}...", Name, ts); return Task.CompletedTask; })
                        .ExecuteAsync(ct => _trader.GetLeftDailyRedemptionQuotaOnFlexibleProductAsync(position.ProductId, type, _cancellation.Token), _cancellation.Token, true);

                    if (quota is not null)
                    {
                        _quotas[(position.ProductId, type)] = (quota, _clock.UtcNow);
                    }
                }
            }

            _updated = !redeeming;

            _logger.LogInformation("{Name} {Asset} cached {Count} quotas", Name, _asset, _quotas.Count);
        }

        private Task TickInvalidateAsync()
        {
            _updated = false;

            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<FlexibleProductPosition>> GetFlexibleProductPositionAsync()
        {
            return Task.FromResult<IReadOnlyCollection<FlexibleProductPosition>>(_positions);
        }

        public Task<LeftDailyRedemptionQuotaOnFlexibleProduct?> TryGetLeftDailyRedemptionQuotaOnFlexibleProductAsync(string productId, FlexibleProductRedemptionType type)
        {
            return Task.FromResult(_quotas.TryGetValue((productId, type), out var value) ? value.Quota : null);
        }

        public async Task RedeemFlexibleProductAsync(string productId, decimal amount, FlexibleProductRedemptionType type)
        {
            // redeem
            await _trader.RedeemFlexibleProductAsync(productId, amount, type);

            // invalidate the cache so we start polling the api
            await TickInvalidateAsync();
        }
    }
}