using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Savings
{
    internal class SavingsGrain : Grain, ISavingsGrain
    {
        private readonly SavingsProviderOptions _options;
        private readonly ILogger _logger;
        private readonly ITradingService _trader;
        private readonly IAlgoDependencyResolver _dependencies;

        public SavingsGrain(IOptions<SavingsProviderOptions> options, ILogger<SavingsGrain> logger, ITradingService trader, IAlgoDependencyResolver dependencies)
        {
            _options = options.Value;
            _logger = logger;
            _trader = trader;
            _dependencies = dependencies;
        }

        private static string TypeName => nameof(SavingsGrain);

        #region Cache

        private readonly Dictionary<string, SavingsProduct> _products = new();

        private readonly Dictionary<string, SavingsPosition> _positions = new();

        private readonly Dictionary<string, SavingsQuota> _quotas = new();

        private readonly CancellationTokenSource _cancellation = new();

        private bool _ready;

        #endregion Cache

        public override async Task OnActivateAsync()
        {
            await UpdateAsync();

            RegisterTimer(_ => UpdateAsync(), null, _options.RefreshPeriod, _options.RefreshPeriod);

            await base.OnActivateAsync();
        }

        public Task<bool> IsReadyAsync() => Task.FromResult(_ready);

        private async Task UpdateAsync()
        {
            // load all products from the exchange
            var products = await GetSavingsProductsAsync();

            // use only the products for assets we care about
            products = products
                .Where(x => _dependencies.AllSymbols.Any(s => s.StartsWith(x.Asset, StringComparison.Ordinal)) || _dependencies.AllSymbols.Any(s => s.EndsWith(x.Asset, StringComparison.Ordinal)))
                .GroupBy(x => x.Asset)
                .Select(x => x.Single());

            // get all positions for each product
            foreach (var product in products)
            {
                // cache the product
                _products[product.Asset] = product;

                await LoadSavingsPositionAsync(product.Asset, product.ProductId);

                await LoadSavingsQuotaAsync(product.Asset, product.ProductId);
            }

            // signal the ready check
            _ready = true;
        }

        public Task<SavingsPosition?> TryGetPositionAsync(string asset)
        {
            var result = _positions.TryGetValue(asset, out var value) ? value : null;

            return Task.FromResult(result);
        }

        public Task<SavingsQuota?> TryGetQuotaAsync(string asset)
        {
            var result = _quotas.TryGetValue(asset, out var value) ? value : null;

            return Task.FromResult(result);
        }

        public async Task<RedeemSavingsEvent> RedeemAsync(string asset, decimal amount)
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

        private async Task<IEnumerable<SavingsProduct>> GetSavingsProductsAsync()
        {
            var watch = Stopwatch.StartNew();

            // get all subscribable products for all assets - there should be only one subscribable per asset
            var products = await _trader
                .WithBackoff()
                .GetSavingsProductsAsync(SavingsStatus.Subscribable, SavingsFeatured.All, _cancellation.Token);

            _logger.LogInformation(
                "{Type} loaded {Count} savings products in {ElapsedMs}ms",
                TypeName, products.Count, watch.ElapsedMilliseconds);

            return products;
        }

        private async Task LoadSavingsPositionAsync(string asset, string productId)
        {
            var watch = Stopwatch.StartNew();

            // get the position for the product
            var positions = await _trader
                .WithBackoff()
                .GetFlexibleProductPositionsAsync(asset, _cancellation.Token);

            var position = positions.SingleOrDefault(x => x.ProductId == productId);
            if (position is not null)
            {
                _positions[position.Asset] = position;
            }

            _logger.LogInformation(
                "{Type} loaded savings position for {Asset} {Product} in {ElapsedMs}ms",
                TypeName, asset, productId, watch.ElapsedMilliseconds);
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
                "{Type} loaded savings quota for {Asset} {ProductId} in {ElapsedMs}ms",
                TypeName, asset, productId, watch.ElapsedMilliseconds);
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
}