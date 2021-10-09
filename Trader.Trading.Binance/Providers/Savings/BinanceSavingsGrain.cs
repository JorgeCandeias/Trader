using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Binance.Providers.Savings
{
    internal class BinanceSavingsGrain : Grain, IBinanceSavingsGrainInternal
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

        private static string Name => nameof(BinanceSavingsGrain);

        private string _asset = string.Empty;
        private ImmutableList<FlexibleProductPosition> _positions = ImmutableList<FlexibleProductPosition>.Empty;

        private readonly CancellationTokenSource _cancellation = new();
        private readonly Dictionary<(string ProductId, FlexibleProductRedemptionType Type), LeftDailyRedemptionQuotaOnFlexibleProduct> _quotas = new();
        private readonly FlexibleProductRedemptionType[] _redemptionTypes = new[] { FlexibleProductRedemptionType.Fast };

        public override async Task OnActivateAsync()
        {
            _asset = this.GetPrimaryKeyString();

            await UpdateAsync();

            // self-update every minute in safe way with incoming calls
            RegisterTimer(_ => this.AsReference<IBinanceSavingsGrainInternal>().UpdateAsync(), default, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

            await base.OnActivateAsync();
        }

        public override Task OnDeactivateAsync()
        {
            _cancellation.Dispose();

            return base.OnDeactivateAsync();
        }

        public async Task UpdateAsync()
        {
            var result = await _trader
                .WithWaitOnTooManyRequests((t, ct) => t
                .GetFlexibleProductPositionAsync(_asset, ct), _logger, _cancellation.Token);

            _positions = result.ToImmutableList();

            _logger.LogInformation("{Name} {Asset} cached {Count} product positions", Name, _asset, _positions.Count);

            foreach (var position in _positions)
            {
                foreach (var type in _redemptionTypes)
                {
                    var quota = await _trader
                        .WithWaitOnTooManyRequests((t, ct) => t
                        .TryGetLeftDailyRedemptionQuotaOnFlexibleProductAsync(position.ProductId, type, ct), _logger, _cancellation.Token);

                    if (quota is not null)
                    {
                        _quotas[(position.ProductId, type)] = quota;
                    }
                }
            }

            _logger.LogInformation("{Name} {Asset} cached {Count} quotas", Name, _asset, _quotas.Count);
        }

        public ValueTask<IReadOnlyCollection<FlexibleProductPosition>> GetFlexibleProductPositionAsync()
        {
            return ValueTask.FromResult<IReadOnlyCollection<FlexibleProductPosition>>(_positions);
        }

        public ValueTask<FlexibleProductPosition?> TryGetFirstFlexibleProductPositionAsync()
        {
            return ValueTask.FromResult(_positions.Count > 0 ? _positions[0] : null);
        }

        public ValueTask<LeftDailyRedemptionQuotaOnFlexibleProduct?> TryGetLeftDailyRedemptionQuotaOnFlexibleProductAsync(string productId, FlexibleProductRedemptionType type)
        {
            return ValueTask.FromResult(_quotas.TryGetValue((productId, type), out var value) ? value : null);
        }

        public async ValueTask RedeemFlexibleProductAsync(string productId, decimal amount, FlexibleProductRedemptionType type)
        {
            await _trader.RedeemFlexibleProductAsync(productId, amount, type);

            await UpdateAsync();
        }
    }

    internal interface IBinanceSavingsGrainInternal : IBinanceSavingsGrain
    {
        Task UpdateAsync();
    }
}