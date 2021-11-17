using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Timers;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Readyness;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Outcompute.Trader.Trading.Providers.Swap;

internal sealed partial class SwapPoolGrain : Grain, ISwapPoolGrain, IDisposable
{
    private readonly IOptionsMonitor<SwapPoolOptions> _monitor;
    private readonly ILogger _logger;
    private readonly ITradingService _trader;
    private readonly ITimerRegistry _timers;
    private readonly IBalanceProvider _balances;
    private readonly ISavingsProvider _savings;
    private readonly ISystemClock _clock;
    private readonly IReadynessProvider _readyness;

    public SwapPoolGrain(IOptionsMonitor<SwapPoolOptions> monitor, ILogger<SwapPoolGrain> logger, ITradingService trader, ITimerRegistry timers, IBalanceProvider balances, ISavingsProvider savings, ISystemClock clock, IReadynessProvider readyness)
    {
        _monitor = monitor;
        _logger = logger;
        _trader = trader;
        _timers = timers;
        _balances = balances;
        _savings = savings;
        _clock = clock;
        _readyness = readyness;
    }

    private static string TypeName => nameof(SwapPoolGrain);

    private readonly CancellationTokenSource _cancellation = new();

    private ImmutableList<SwapPool> _pools = ImmutableList<SwapPool>.Empty;

    private readonly Dictionary<long, SwapPoolLiquidity> _liquidities = new();

    private readonly Dictionary<long, SwapPoolConfiguration> _configurations = new();

    private readonly Dictionary<long, DateTime> _cooldowns = new();

    private bool _ready;

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
        // skip if the system is not ready
        if (!await _readyness.IsReadyAsync())
        {
            return;
        }

        // load all the pool information
        await LoadAsync();

        // skip management if auto pooling is disabled
        var options = _monitor.CurrentValue;
        if (!options.AutoAddEnabled)
        {
            return;
        }

        // get all positive spot balances
        var spots = (await _balances.GetBalancesAsync(_cancellation.Token))
            .ToDictionary(x => x.Asset);

        LogPositiveSpotBalances(TypeName, spots.Count);

        // get user savings positions for known pools
        var savings = (await _savings.GetBalancesAsync())
            .ToDictionary(x => x.Asset);

        LogPositiveSavingsBalances(TypeName, savings.Count);

        var totals = _configurations.Values
            .SelectMany(x => x.Assets.Keys)
            .Distinct()
            .ToDictionary(x => x, x => (spots.GetValueOrDefault(x)?.Free ?? 0m) + (savings.GetValueOrDefault(x)?.FreeAmount ?? 0m));

        LogAssetsWithUsableAmounts(TypeName, totals.Count);

        // select pools to which we can add assets
        var candidates = _configurations.Values
            .Where(x => options.Assets.IsSupersetOf(x.Assets.Keys))
            .Where(x => _cooldowns.GetValueOrDefault(x.PoolId, DateTime.MinValue) < _clock.UtcNow)
            .Where(x => x.Assets.All(a => totals[a.Key] >= a.Value.MinAdd))
            .OrderByDescending(x => x.PoolId)
            .ToList();

        LogCandidatePools(TypeName, candidates.Count);

        // test all pools until we match one
        foreach (var candidate in candidates)
        {
            // test all assets in the pool to ensure they fit requirements
            foreach (var asset in candidate.Assets)
            {
                // request a preview using the current asset as quote asset
                var preview = await _trader.AddSwapPoolLiquidityPreviewAsync(candidate.PoolId, SwapPoolLiquidityType.Combination, asset.Key, totals[asset.Key], _cancellation.Token);

                // check if the paired asset quantity is also above the min after preview
                if (preview.BaseAmount < candidate.Assets[preview.BaseAsset].MinAdd)
                {
                    continue;
                }

                // check if there is enough quote asset usable
                var quoteUsable = totals.GetValueOrDefault(preview.QuoteAsset, 0m);
                if (quoteUsable < preview.QuoteAmount)
                {
                    continue;
                }

                // check if there is enough base asset usable
                var baseUsable = totals.GetValueOrDefault(preview.BaseAsset, 0m);
                if (baseUsable < preview.BaseAmount)
                {
                    continue;
                }

                LogElectedPool(TypeName, candidate.PoolName, preview.QuoteAmount, preview.QuoteAsset, preview.BaseAmount, preview.BaseAsset);

                // ensure there is enough spot amount for the quote asset by redeeming savings
                var (quoteSuccess, quoteRedeemed) = await EnsureSpotAmountAsync(preview.QuoteAsset, preview.QuoteAmount, spots.GetValueOrDefault(preview.QuoteAsset)?.Free ?? 0m, savings.GetValueOrDefault(preview.QuoteAsset)?.FreeAmount ?? 0m);
                if (!quoteSuccess)
                {
                    continue;
                }

                // ensure there is enough spot amount for the base asset by redeeming savings
                var (baseSuccess, baseRedeemed) = await EnsureSpotAmountAsync(preview.BaseAsset, preview.BaseAmount, spots.GetValueOrDefault(preview.BaseAsset)?.Free ?? 0m, savings.GetValueOrDefault(preview.BaseAsset)?.FreeAmount ?? 0m);
                if (!baseSuccess)
                {
                    continue;
                }

                // if any savings were redeem then let the timer cycle to allow the exchange to complete the operation
                if (quoteRedeemed || baseRedeemed) return;

                // if we got here we can add liquidity
                await _trader.AddSwapLiquidityAsync(candidate.PoolId, SwapPoolLiquidityType.Combination, preview.QuoteAsset, preview.QuoteAmount, _cancellation.Token);

                // bump the cooldown for this pool
                SetCooldown(candidate.PoolId);

                // allow the schedule to cycle
                return;
            }
        }
    }

    private async ValueTask<(bool Success, bool Redeemed)> EnsureSpotAmountAsync(string asset, decimal targetAmount, decimal spotAmount, decimal savingsAmount)
    {
        if (spotAmount >= targetAmount)
        {
            return (true, false);
        }

        var necessary = targetAmount - spotAmount;

        if (!_monitor.CurrentValue.AutoRedeemSavings)
        {
            LogCannotRedeemNecessaryAutoRedemptionDisabled(TypeName, necessary, asset);

            return (false, false);
        }

        if (savingsAmount < necessary)
        {
            LogCannotRedeemNecessaryNotEnoughAvailable(TypeName, necessary, asset, savingsAmount);

            return (false, false);
        }

        var result = await _savings.RedeemAsync(asset, necessary, _cancellation.Token);
        if (result.Success)
        {
            LogRedeemedFromSavingsToCoverNecessary(TypeName, result.Redeemed, asset, necessary);

            return (true, true);
        }
        else
        {
            LogFailedToRedeemNecessaryUnknownReasons(TypeName, necessary, asset);

            return (false, false);
        }
    }

    public async ValueTask<RedeemSwapPoolEvent> RedeemAsync(string asset, decimal amount)
    {
        // identify a pool with enough asset share
        var result = _liquidities.Values
            .Where(x => x.AssetShare.GetValueOrDefault(asset, 0m) >= amount)
            .OrderBy(x => x.PoolId)
            .FirstOrDefault();

        if (result is null)
        {
            LogFailedToRedeemNoPoolAvailable(TypeName, amount, asset);

            return RedeemSwapPoolEvent.Failed(asset);
        }

        var cooldown = _cooldowns.GetValueOrDefault(result.PoolId, DateTime.MinValue);
        if (cooldown > _clock.UtcNow)
        {
            LogFailedToRedeemPoolOnCooldown(TypeName, amount, asset, result.PoolName, cooldown);

            return RedeemSwapPoolEvent.Failed(asset);
        }

        // calculate the share fraction to redeem
        var fraction = (amount / result.AssetShare[asset]) * result.ShareAmount;
        fraction = Math.Round(fraction, 8);

        // bump the fraction up to the minimum share redeemable
        fraction = Math.Max(fraction, _configurations[result.PoolId].Liquidity.MinShareRedemption);

        // bump the fraction down to the full user share available
        fraction = Math.Min(fraction, result.ShareAmount);

        // for reporting only
        var baseAsset = result.AssetShare.Single(x => x.Key != asset).Key;
        var baseAmount = fraction * result.AssetShare[baseAsset];

        // redeem the fraction
        await _trader.RemoveSwapLiquidityAsync(result.PoolId, SwapPoolLiquidityType.Combination, fraction, _cancellation.Token);

        SetCooldown(result.PoolId);

        return new RedeemSwapPoolEvent(true, result.PoolName, asset, amount, baseAsset, baseAmount);
    }

    public ValueTask<SwapPoolAssetBalance> GetBalanceAsync(string asset)
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

        return ValueTask.FromResult(header);
    }

    public ValueTask<IEnumerable<SwapPool>> GetSwapPoolsAsync()
    {
        return ValueTask.FromResult<IEnumerable<SwapPool>>(_pools);
    }

    public ValueTask<IEnumerable<SwapPoolConfiguration>> GetSwapPoolConfigurationsAsync()
    {
        return ValueTask.FromResult<IEnumerable<SwapPoolConfiguration>>(_configurations.Values.ToImmutableList());
    }

    public void Dispose()
    {
        _cancellation.Dispose();
    }

    private async Task LoadAsync()
    {
        await LoadSwapPoolsAsync();
        await LoadSwapPoolLiquiditiesAsync();
        await LoadSwapPoolConfigurationsAsync();

        _ready = true;
    }

    private async Task LoadSwapPoolsAsync()
    {
        var watch = Stopwatch.StartNew();

        var pools = await _trader.GetSwapPoolsAsync(_cancellation.Token);

        _pools = pools.ToImmutableList();

        LogLoadedSwapPoolsInMs(TypeName, _pools.Count, watch.ElapsedMilliseconds);
    }

    private async Task LoadSwapPoolLiquiditiesAsync()
    {
        var watch = Stopwatch.StartNew();

        var liquidities = await _trader.GetSwapLiquiditiesAsync(_cancellation.Token);

        _liquidities.ReplaceWith(liquidities, x => x.PoolId);

        LogLoadedSwapPoolLiquiditiesInMs(TypeName, _liquidities.Count, watch.ElapsedMilliseconds);
    }

    private async Task LoadSwapPoolConfigurationsAsync()
    {
        var watch = Stopwatch.StartNew();

        var configurations = await _trader.GetSwapPoolConfigurationsAsync(_cancellation.Token);

        _configurations.ReplaceWith(configurations, x => x.PoolId);

        LogLoadedSwapPoolConfigurationsInMs(TypeName, _configurations.Count, watch.ElapsedMilliseconds);
    }

    private void SetCooldown(long poolId)
    {
        _cooldowns[poolId] = _clock.UtcNow.Add(_monitor.CurrentValue.PoolCooldown);
    }

    public ValueTask<bool> IsReadyAsync() => ValueTask.FromResult(_ready);

    #region Logging

    [LoggerMessage(0, LogLevel.Information, "{Type} reports {Count} positive spot balances")]
    private partial void LogPositiveSpotBalances(string type, int count);

    [LoggerMessage(1, LogLevel.Information, "{Type} reports {Count} positive savings balances")]
    private partial void LogPositiveSavingsBalances(string type, int count);

    [LoggerMessage(2, LogLevel.Information, "{Type} reports {Count} assets with usable amounts")]
    private partial void LogAssetsWithUsableAmounts(string type, int count);

    [LoggerMessage(3, LogLevel.Information, "{Type} identified {Count} candidate pools")]
    private partial void LogCandidatePools(string type, int count);

    [LoggerMessage(4, LogLevel.Information, "{Type} elected pool {PoolName} for adding assets {QuoteAmount:F8} {QuoteAsset} and {BaseAmount:F8} {BaseAsset}")]
    private partial void LogElectedPool(string type, string poolName, decimal quoteAmount, string quoteAsset, decimal baseAmount, string baseAsset);

    [LoggerMessage(5, LogLevel.Information, "{Type} cannot redeem necessary {Necessary:F8} {Asset} from savings because auto redemption is disabled")]
    private partial void LogCannotRedeemNecessaryAutoRedemptionDisabled(string type, decimal necessary, string asset);

    [LoggerMessage(6, LogLevel.Error, "{Type} cannot redeem necessary {Necessary:F8} {Asset} from savings because there is only {Position:F8} {Asset} available")]
    private partial void LogCannotRedeemNecessaryNotEnoughAvailable(string type, decimal necessary, string asset, decimal position);

    [LoggerMessage(7, LogLevel.Information, "{Type} redeemed {Redeemed:F8} {Asset} from savings to cover the necessary {Necessary:F8} {Asset}")]
    private partial void LogRedeemedFromSavingsToCoverNecessary(string type, decimal redeemed, string asset, decimal necessary);

    [LoggerMessage(8, LogLevel.Error, "{Type} failed to redeem the necessary {Necessary:F8} {Asset} from savings due to unknown reasons")]
    private partial void LogFailedToRedeemNecessaryUnknownReasons(string type, decimal necessary, string asset);

    [LoggerMessage(9, LogLevel.Error, "{Type} failed to redeem {Amount} {Asset} because no available pool can be found")]
    private partial void LogFailedToRedeemNoPoolAvailable(string type, decimal amount, string asset);

    [LoggerMessage(10, LogLevel.Warning, "{Type} failed to redeem {Amount} {Asset} from pool {PoolName} is on cooldown until {Cooldown}")]
    private partial void LogFailedToRedeemPoolOnCooldown(string type, decimal amount, string asset, string poolName, DateTime cooldown);

    [LoggerMessage(11, LogLevel.Information, "{Type} loaded {Count} Swap Pools in {ElapsedMs}ms")]
    private partial void LogLoadedSwapPoolsInMs(string type, int count, long elapsedMs);

    [LoggerMessage(12, LogLevel.Information, "{Type} loaded {Count} Swap Pool Liquidity details in {ElapsedMs}ms")]
    private partial void LogLoadedSwapPoolLiquiditiesInMs(string type, int count, long elapsedMs);

    [LoggerMessage(13, LogLevel.Information, "{Type} loaded {Count} Swap Pool Configuration details in {ElapsedMs}ms")]
    private partial void LogLoadedSwapPoolConfigurationsInMs(string type, int count, long elapsedMs);

    #endregion Logging
}