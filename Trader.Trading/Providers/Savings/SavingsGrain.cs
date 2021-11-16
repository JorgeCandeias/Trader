using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Outcompute.Trader.Trading.Providers.Savings;

internal class SavingsGrain : Grain, ISavingsGrain
{
    private readonly SavingsProviderOptions _options;
    private readonly ILogger _logger;
    private readonly ITradingService _trader;
    private readonly IAlgoDependencyResolver _dependencies;
    private readonly ISystemClock _clock;
    private readonly IExchangeInfoProvider _exchange;
    private readonly IHostApplicationLifetime _lifetime;

    public SavingsGrain(IOptions<SavingsProviderOptions> options, ILogger<SavingsGrain> logger, ITradingService trader, IAlgoDependencyResolver dependencies, ISystemClock clock, IExchangeInfoProvider exchange, IHostApplicationLifetime lifetime)
    {
        _options = options.Value;
        _logger = logger;
        _trader = trader;
        _dependencies = dependencies;
        _clock = clock;
        _exchange = exchange;
        _lifetime = lifetime;
    }

    private static string TypeName => nameof(SavingsGrain);

    #region Cache

    private IReadOnlyList<SavingsProduct> _products = ImmutableList<SavingsProduct>.Empty;

    private readonly Dictionary<string, SavingsPosition> _positions = new();

    private readonly Dictionary<string, SavingsQuota> _quotas = new();

    private readonly CancellationTokenSource _cancellation = new();

    private bool _ready;

    private Task? _loadTask;

    private DateTime _nextLoad = DateTime.MinValue;

    #endregion Cache

    public override async Task OnActivateAsync()
    {
        RegisterTimer(_ => EnsureLoadAsync(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

        await base.OnActivateAsync();
    }

    public override Task OnDeactivateAsync()
    {
        _cancellation.Cancel();

        return base.OnDeactivateAsync();
    }

    public ValueTask<bool> IsReadyAsync() => ValueTask.FromResult(_ready);

    private async Task EnsureLoadAsync()
    {
        // start loading at the apropriate period
        if (_loadTask is null && _clock.UtcNow >= _nextLoad)
        {
            _loadTask = Task.Run(() => LoadAsync(), _cancellation.Token);
            return;
        }

        // observe exceptions
        if (_loadTask is not null && _loadTask.IsCompleted)
        {
            try
            {
                await _loadTask;
            }
            finally
            {
                _loadTask = null;
                _nextLoad = _clock.UtcNow.Add(_options.RefreshPeriod);
            }
        }
    }

    private async Task LoadAsync()
    {
        // load all products
        _products = (await _trader.GetSavingsProductsAsync(SavingsStatus.All, SavingsFeatured.All, _lifetime.ApplicationStopping))
            .ToImmutableList();

        // load positions for used assets only
        // note that some products can have positions yet no declaring product (e.g. bnb on binance)
        var assets = new HashSet<string>();
        foreach (var name in _dependencies.AllSymbols)
        {
            var symbol = _exchange.GetRequiredSymbol(name);
            assets.Add(symbol.BaseAsset);
            assets.Add(symbol.QuoteAsset);
        }

        // get all positions for each asset
        foreach (var asset in assets)
        {
            var position = await LoadSavingsPositionAsync(asset);

            if (position is not null)
            {
                await LoadSavingsQuotaAsync(asset, position.ProductId);
            }
        }

        // signal the ready check
        _ready = true;

        _logger.LogInformation("{Type} is ready", TypeName);
    }

    public ValueTask<IReadOnlyList<SavingsProduct>> GetProductsAsync() => ValueTask.FromResult(_products);

    public ValueTask<IEnumerable<SavingsPosition>> GetPositionsAsync()
    {
        return ValueTask.FromResult<IEnumerable<SavingsPosition>>(_positions.Values.ToImmutableList());
    }

    public ValueTask<SavingsPosition?> TryGetPositionAsync(string asset)
    {
        var result = _positions.TryGetValue(asset, out var value) ? value : null;

        return ValueTask.FromResult(result);
    }

    public ValueTask<SavingsQuota?> TryGetQuotaAsync(string asset)
    {
        var result = _quotas.TryGetValue(asset, out var value) ? value : null;

        return ValueTask.FromResult(result);
    }

    public async ValueTask<RedeemSavingsEvent> RedeemAsync(string asset, decimal amount)
    {
        // get the current savings for the asset
        if (!_positions.TryGetValue(asset, out var position))
        {
            _logger.LogWarning(
                "{Type} cannot redeem savings for asset {Asset} because there is no savings product",
                TypeName, asset);

            return new RedeemSavingsEvent(false, 0m);
        }

        // check if we can redeem at all - we cant redeem during maintenance windows etc
        if (!position.CanRedeem)
        {
            _logger.LogWarning(
                "{Type} cannot redeem savings at this time because redeeming is disallowed",
                TypeName);

            return new RedeemSavingsEvent(false, 0m);
        }

        // check if there is a redemption in progress
        if (position.RedeemingAmount > 0)
        {
            _logger.LogWarning(
                "{Type} will not redeem savings now because a redemption of {RedeemingAmount} {Asset} is in progress",
                TypeName, position.RedeemingAmount, asset);

            return new RedeemSavingsEvent(false, 0m);
        }

        // check if there is enough for redemption
        if (position.FreeAmount < amount)
        {
            _logger.LogError(
                "{Type} cannot redeem the necessary {Quantity} {Asset} from savings because they only contain {FreeAmount} {Asset}",
                TypeName, amount, asset, position.FreeAmount, asset);

            return new RedeemSavingsEvent(false, 0m);
        }

        var quota = _quotas.TryGetValue(asset, out var value) ? value : SavingsQuota.Empty;

        // stop if we would exceed the daily quota outright
        if (quota.LeftQuota < amount)
        {
            _logger.LogError(
                "{Type} cannot redeem the necessary amount of {Quantity} {Asset} because it exceeds the available quota of {Quota} {Asset}",
                TypeName, amount, asset, quota.LeftQuota, asset);

            return new RedeemSavingsEvent(false, 0m);
        }

        // bump the necessary value if needed now
        if (amount < quota.MinRedemptionAmount)
        {
            var bumped = Math.Min(quota.MinRedemptionAmount, position.FreeAmount);

            _logger.LogInformation(
                "{Type} bumped the necessary quantity of {Necessary} {Asset} to {Bumped} {Asset} to enable redemption",
                TypeName, amount, asset, bumped, asset);

            amount = bumped;
        }

        // if we got here then we can attempt to redeem
        _logger.LogInformation(
            "{Type} attempting to redeem {Quantity} {Asset} from savings...",
            TypeName, amount, asset);

        await _trader.RedeemFlexibleProductAsync(position.ProductId, amount, SavingsRedemptionType.Fast);

        AdjustCachedAmounts(asset, -amount);

        _logger.LogInformation(
            "{Type} redeemed {Quantity} {Asset} from savings",
            TypeName, amount, asset);

        return new RedeemSavingsEvent(true, amount);
    }

    private async Task<SavingsPosition?> LoadSavingsPositionAsync(string asset)
    {
        var watch = Stopwatch.StartNew();

        // get the position for the product
        var positions = await _trader
            .WithBackoff()
            .GetFlexibleProductPositionsAsync(asset, _cancellation.Token);

        var position = positions.SingleOrDefault();
        if (position is not null)
        {
            _positions[position.Asset] = position;
        }

        _logger.LogInformation(
            "{Type} loaded savings position for {Asset} in {ElapsedMs}ms",
            TypeName, asset, watch.ElapsedMilliseconds);

        return position;
    }

    private async Task LoadSavingsQuotaAsync(string asset, string productId)
    {
        var watch = Stopwatch.StartNew();

        // get the quota for the product
        var quota = await _trader
            .WithBackoff()
            .TryGetLeftDailyRedemptionQuotaOnFlexibleProductAsync(productId, SavingsRedemptionType.Fast, _cancellation.Token);

        if (quota is not null)
        {
            _quotas[quota.Asset] = quota;
        }

        _logger.LogInformation(
            "{Type} loaded savings quota for {Asset} in {ElapsedMs}ms",
            TypeName, asset, watch.ElapsedMilliseconds);
    }

    private void AdjustCachedAmounts(string asset, decimal amount)
    {
        AdjustCachedPosition(asset, amount);
        AdjustCachedQuota(asset, amount);
    }

    private void AdjustCachedPosition(string asset, decimal amount)
    {
        if (_positions.TryGetValue(asset, out var position))
        {
            _positions[asset] = position with { FreeAmount = position.FreeAmount + amount };
        }
    }

    private void AdjustCachedQuota(string asset, decimal amount)
    {
        if (_quotas.TryGetValue(asset, out var quota))
        {
            _quotas[asset] = quota with { LeftQuota = quota.LeftQuota - amount };
        }
    }
}