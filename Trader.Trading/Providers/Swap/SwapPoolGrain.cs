using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Timers;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Swap
{
    internal sealed class SwapPoolGrain : Grain, ISwapPoolGrain, IDisposable
    {
        private readonly IOptionsMonitor<SwapPoolOptions> _monitor;
        private readonly ILogger _logger;
        private readonly ITradingService _trader;
        private readonly ITimerRegistry _timers;
        private readonly IBalanceProvider _balances;
        private readonly ISavingsProvider _savings;
        private readonly ISystemClock _clock;

        public SwapPoolGrain(IOptionsMonitor<SwapPoolOptions> monitor, ILogger<SwapPoolGrain> logger, ITradingService trader, ITimerRegistry timers, IBalanceProvider balances, ISavingsProvider savings, ISystemClock clock)
        {
            _monitor = monitor;
            _logger = logger;
            _trader = trader;
            _timers = timers;
            _balances = balances;
            _savings = savings;
            _clock = clock;
        }

        private static string TypeName => nameof(SwapPoolGrain);

        private readonly CancellationTokenSource _cancellation = new();

        private readonly Dictionary<long, SwapPool> _pools = new();

        private readonly Dictionary<long, SwapPoolLiquidity> _liquidities = new();

        private readonly Dictionary<long, SwapPoolConfiguration> _configurations = new();

        private readonly Dictionary<long, DateTime> _cooldowns = new();

        public override async Task OnActivateAsync()
        {
            await LoadAsync();

            _timers.RegisterTimer(this, _ => TickAsync(), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

            await base.OnActivateAsync();
        }

        public override Task OnDeactivateAsync()
        {
            _cancellation.Cancel();

            return base.OnDeactivateAsync();
        }

        private async Task TickAsync()
        {
            var options = _monitor.CurrentValue;

            // skip if auto pooling is disabled
            if (!options.AutoAddEnabled)
            {
                return;
            }

            await LoadAsync();

            // get all positive spot balances
            var spots = (await _balances.GetBalancesAsync(_cancellation.Token))
                .Where(x => x.Free > 0)
                .ToDictionary(x => x.Asset);

            _logger.LogInformation(
                "{Type} reports {Count} positive spot balances",
                TypeName, spots.Count);

            // get user savings positions for known pools
            var savings = new Dictionary<string, SavingsPosition>();
            foreach (var asset in _configurations.Values.SelectMany(x => x.Assets.Keys).Distinct())
            {
                // todo: refactor the savings layer so we can get all savings in one go
                var position = await _savings.GetPositionOrZeroAsync(asset, _cancellation.Token);
                if (position.FreeAmount > 0)
                {
                    savings[position.Asset] = position;
                }
            }

            _logger.LogInformation(
                "{Type} reports {Count} positive savings balances",
                TypeName, savings.Count);

            var totals = new Dictionary<string, decimal>();
            foreach (var asset in _configurations.Values.SelectMany(x => x.Assets.Keys).Distinct())
            {
                var spotAmount = spots.TryGetValue(asset, out var spot) ? spot.Free : 0m;
                var savingsAmount = savings.TryGetValue(asset, out var position) ? position.FreeAmount : 0m;
                var total = spotAmount + savingsAmount;
                if (total > 0)
                {
                    totals[asset] = total;
                }
            }

            _logger.LogInformation(
                "{Type} reports {Count} assets with usable amounts",
                TypeName, totals.Count);

            // select pools to which we can add assets
            var candidates = _configurations.Values
                .Where(x => _cooldowns.GetValueOrDefault(x.PoolId, DateTime.MinValue) < _clock.UtcNow)
                .Where(x => x.Assets.All(a => totals.TryGetValue(a.Key, out var balance) && balance >= a.Value.MinAdd))
                .Where(x => !options.ExcludedAssets.Overlaps(x.Assets.Keys))
                .Where(x => !x.Assets.All(a => options.IsolatedAssets.Contains(a.Key)))
                .OrderByDescending(x => x.PoolId)
                .ToList();

            _logger.LogInformation(
                "{Type} identified {Count} candidate pools",
                TypeName, candidates.Count);

            // test all pools until we match one
            foreach (var candidate in candidates)
            {
                // test all assets in the pool to ensure they fit requirements
                foreach (var asset in candidate.Assets)
                {
                    // request a preview using the current asset as quote asset
                    var preview = await _trader.AddSwapPoolLiquidityPreviewAsync(candidate.PoolId, SwapPoolLiquidityType.Combination, asset.Key, totals[asset.Key], _cancellation.Token);

                    // check if the paired asset quantity is also above the min after preview
                    if (preview.BaseAmount >= candidate.Assets[preview.BaseAsset].MinAdd)
                    {
                        // check if there is enough free balance for the base asset as per preview
                        if (preview.BaseAmount <= totals[preview.BaseAsset])
                        {
                            _logger.LogInformation(
                                "{Type} elected pool {PoolName} for adding assets {QuoteAmount:F8} {QuoteAsset} and {BaseAmount:F8} {BaseAsset}",
                                TypeName, candidate.PoolName, preview.QuoteAmount, preview.QuoteAsset, preview.BaseAmount, preview.BaseAsset);

                            // check if there is enough quote asset in the spot account
                            {
                                var spotAmount = spots.TryGetValue(preview.QuoteAsset, out var spot) ? spot.Free : 0m;
                                if (spotAmount < preview.QuoteAmount)
                                {
                                    var position = savings[preview.QuoteAsset];
                                    var diff = preview.QuoteAmount - spotAmount;
                                    await _savings.RedeemAsync(position.Asset, diff, SavingsRedemptionType.Fast, _cancellation.Token);

                                    _logger.LogInformation(
                                        "{Type} redeemed necessary {Necessary:F8} {Asset} from savings",
                                        TypeName, diff, position.Asset);

                                    // allow the schedule to cycle
                                    return;
                                }
                            }

                            // check if there is enough base asset in the spot account
                            {
                                var spotAmount = spots.TryGetValue(preview.BaseAsset, out var spot) ? spot.Free : 0m;
                                if (spotAmount < preview.BaseAmount)
                                {
                                    var position = savings[preview.BaseAsset];
                                    var diff = preview.BaseAmount - spotAmount;
                                    await _savings.RedeemAsync(position.Asset, diff, SavingsRedemptionType.Fast, _cancellation.Token);

                                    _logger.LogInformation(
                                        "{Type} redeemed necessary {Necessary:F8} {Asset} from savings",
                                        TypeName, diff, position.Asset);

                                    // allow the schedule to cycle
                                    return;
                                }
                            }

                            // if we got here we can add liquidity
                            await _trader.AddSwapLiquidityAsync(candidate.PoolId, SwapPoolLiquidityType.Combination, preview.QuoteAsset, preview.QuoteAmount, _cancellation.Token);

                            // bump the cooldown for this pool
                            SetCooldown(candidate.PoolId);

                            // allow the schedule to cycle
                            return;
                        }
                    }
                }
            }
        }

        public async Task<RedeemSwapPoolEvent> RedeemAsync(string asset, decimal amount)
        {
            // identify a pool with enough asset share
            var result = _liquidities.Values
                .Where(x => x.AssetShare.TryGetValue(asset, out var share) && share >= amount)
                .Where(x => _cooldowns.GetValueOrDefault(x.PoolId, DateTime.MinValue) < _clock.UtcNow)
                .OrderBy(x => x.PoolId)
                .FirstOrDefault();

            if (result is null)
            {
                _logger.LogWarning(
                    "{Type} could not redeem asset {Amount} {Asset} because available pool can be found",
                    TypeName, amount, asset);

                return RedeemSwapPoolEvent.Failed(asset);
            }

            // calculate the share fraction to redeem
            var fraction = (amount / result.AssetShare[asset]) * result.ShareAmount;
            fraction = Math.Round(fraction, 8);

            // for reporting only
            var baseAsset = result.AssetShare.Single(x => x.Key != asset).Key;
            var baseAmount = fraction * result.AssetShare[baseAsset];

            // redeem the fraction
            await _trader.RemoveSwapLiquidityAsync(result.PoolId, SwapPoolLiquidityType.Combination, fraction, _cancellation.Token);

            SetCooldown(result.PoolId);

            return new RedeemSwapPoolEvent(true, result.PoolName, asset, amount, baseAsset, baseAmount);
        }

        public Task<SwapPoolAssetBalance> GetBalanceAsync(string asset)
        {
            var builder = ImmutableList.CreateBuilder<SwapPoolAssetBalanceDetail>();

            foreach (var liquidity in _liquidities.Values)
            {
                foreach (var share in liquidity.AssetShare)
                {
                    if (share.Key == asset)
                    {
                        builder.Add(new SwapPoolAssetBalanceDetail(liquidity.PoolName, share.Value));
                    }
                }
            }

            var header = new SwapPoolAssetBalance(asset, builder.Sum(x => x.Amount), builder.ToImmutable());

            return Task.FromResult(header);
        }

        public Task PingAsync() => Task.CompletedTask;

        public void Dispose()
        {
            _cancellation.Dispose();
        }

        private async Task LoadAsync()
        {
            await LoadSwapPoolsAsync();
            await LoadSwapPoolLiquiditiesAsync();
            await LoadSwapPoolConfigurationsAsync();
        }

        private async Task LoadSwapPoolsAsync()
        {
            var watch = Stopwatch.StartNew();

            var pools = await _trader.GetSwapPoolsAsync(_cancellation.Token);

            _pools.ReplaceWith(pools, x => x.PoolId);

            _logger.LogInformation("{Type} loaded {Count} Swap Pools in {ElapsedMs}ms", TypeName, _pools.Count, watch.ElapsedMilliseconds);
        }

        private async Task LoadSwapPoolLiquiditiesAsync()
        {
            var watch = Stopwatch.StartNew();

            var liquidities = await _trader.GetSwapLiquiditiesAsync(_cancellation.Token);

            _liquidities.ReplaceWith(liquidities, x => x.PoolId);

            _logger.LogInformation("{Type} loaded {Count} Swap Pool Liquidity details in {ElapsedMs}ms", TypeName, _liquidities.Count, watch.ElapsedMilliseconds);
        }

        private async Task LoadSwapPoolConfigurationsAsync()
        {
            var watch = Stopwatch.StartNew();

            var configurations = await _trader.GetSwapPoolConfigurationsAsync(_cancellation.Token);

            _configurations.ReplaceWith(configurations, x => x.PoolId);

            _logger.LogInformation("{Type} loaded {Count} Swap Pool Configuration details in {ElapsedMs}ms", TypeName, _configurations.Count, watch.ElapsedMilliseconds);
        }

        private void SetCooldown(long poolId)
        {
            _cooldowns[poolId] = _clock.UtcNow.Add(_monitor.CurrentValue.PoolCooldown);
        }
    }
}