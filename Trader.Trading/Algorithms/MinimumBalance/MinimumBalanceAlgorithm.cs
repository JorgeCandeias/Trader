using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Exceptions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms.MinimumBalance
{
    internal class MinimumBalanceAlgorithm : ISymbolAlgo
    {
        private readonly MinimumBalanceAlgorithmOptions _options;
        private readonly ILogger _logger;
        private readonly ITradingRepository _repository;
        private readonly ITradingService _trader;

        public MinimumBalanceAlgorithm(string name, IOptionsSnapshot<MinimumBalanceAlgorithmOptions> options, ILogger<MinimumBalanceAlgorithm> logger, ITradingRepository repository, ITradingService trader)
        {
            _options = options.Get(name);
            _logger = logger;
            _repository = repository;
            _trader = trader;
        }

        private FlexibleProduct _product;
        private decimal _minRedemptionAmount;

        public string Symbol => throw new System.NotImplementedException();

        public Task<Profit> GetProfitAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Profit.Zero(string.Empty, string.Empty, _options.Asset));
        }

        public Task<Statistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Statistics.Zero);
        }

        public async Task InitializeAsync(ExchangeInfo exchangeInfo, CancellationToken cancellationToken = default)
        {
            _product = _trader.GetCachedFlexibleProductsByAsset(_options.Asset).Single();

            var quota = await _trader
                .GetLeftDailyRedemptionQuotaOnFlexibleProductAsync(_product.ProductId, FlexibleProductRedemptionType.Fast, cancellationToken)
                .ConfigureAwait(false);

            if (quota is null)
            {
                throw new AlgorithmException($"Cannot get current quota for product '{_product.ProductId}' of asset '{_product.Asset}'");
            }

            _minRedemptionAmount = quota.MinRedemptionAmount;
        }

        public async Task GoAsync(CancellationToken cancellationToken = default)
        {
            // get the current state
            var current = await GetCurrentStateAsync(cancellationToken).ConfigureAwait(false);

            // get the desired state
            var desired = GetDesiredState(current);

            // get the change required
            var change = GetChange(current, desired);

            // apply the change
            await ApplyAsync(change, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("{Type} {Name} has applied the change", nameof(MinimumBalanceAlgorithm), _options.Asset);
        }

        private async Task<decimal> GetCurrentStateAsync(CancellationToken cancellationToken)
        {
            var balance = await _repository
                .GetBalanceAsync(_options.Asset, cancellationToken)
                .ConfigureAwait(false);

            if (balance is null)
            {
                throw new AlgorithmException($"Cannot get current balance for asset '{_options.Asset}'");
            }

            _logger.LogInformation("{Type} {Name} reports current balance is {Current}", nameof(MinimumBalanceAlgorithm), _options.Asset, balance.Free);

            return balance.Free;
        }

        private decimal GetDesiredState(decimal current)
        {
            var desired = Math.Max(current, _options.MinimumBalance);

            _logger.LogInformation("{Type} {Name} reports desired balance is {Desired}", nameof(MinimumBalanceAlgorithm), _options.Asset, desired);

            return desired;
        }

        private decimal GetChange(decimal current, decimal desired)
        {
            var change = desired - current;

            // stop if there no change to apply
            if (change is 0)
            {
                _logger.LogInformation(
                    "{Type} {Name} reports no change required",
                    nameof(MinimumBalanceAlgorithm), _options.Asset);

                return change;
            }
            else
            {
                _logger.LogInformation(
                    "{Type} {Name} reports change required of {Change} {Asset}",
                    nameof(MinimumBalanceAlgorithm), _options.Asset, change, _options.Asset);
            }

            // raise to the minimum redemption amount
            if (change < _minRedemptionAmount)
            {
                change = Math.Max(change, _minRedemptionAmount);

                _logger.LogInformation(
                    "{Type} {Name} raised change to minimum redemption amount of {Amount} {Asset}",
                    nameof(MinimumBalanceAlgorithm), _options.Asset, change, _options.Asset);
            }

            return change;
        }

        private async Task ApplyAsync(decimal change, CancellationToken cancellationToken)
        {
            // stop if no change is required
            if (change <= 0)
            {
                _logger.LogInformation(
                    "{Type} {Name} reports redemption not necessary",
                    nameof(MinimumBalanceAlgorithm), _options.Asset);

                return;
            }

            // get the current quota to perform additional checks
            var quota = await _trader
                .GetLeftDailyRedemptionQuotaOnFlexibleProductAsync(_product.ProductId, FlexibleProductRedemptionType.Fast, cancellationToken)
                .ConfigureAwait(false);

            if (quota is null)
            {
                throw new AlgorithmException($"Cannot get current quota for product '{_product.ProductId}' of asset '{_product.Asset}'");
            }

            // check if the change is under the quota
            if (change > quota.LeftQuota)
            {
                _logger.LogError(
                    "{Type} {Name} cannot redeem {Change} {Asset} because it is over the available quota of {Quota} {Asset}",
                    nameof(MinimumBalanceAlgorithm), _options.Asset, change, _options.Asset, quota.LeftQuota, _options.Asset);

                return;
            }

            // get the flexible product position to perform additional checks
            var positions = await _trader
                .GetFlexibleProductPositionAsync(_options.Asset, cancellationToken)
                .ConfigureAwait(false);

            var position = positions.SingleOrDefault();

            // check if the there is a product at all
            if (position is null)
            {
                _logger.LogError(
                    "{Type} {Name} cannot redeem {Change} {Asset} because there is no savings product for {Asset}",
                    nameof(MinimumBalanceAlgorithm), _options.Asset, change, _options.Asset, _options.Asset);

                return;
            }

            // check if we can redeem the product
            if (!position.CanRedeem)
            {
                _logger.LogError(
                    "{Type} {Name} cannot redeem {Change} {Asset} because redemption is not allowed at this time",
                    nameof(MinimumBalanceAlgorithm), _options.Asset, change, _options.Asset);

                return;
            }

            // check if there is a redemption in progress
            if (position.RedeemingAmount > 0m)
            {
                _logger.LogError(
                    "{Type} {Name} cannot redeem {Change} {Asset} because there is a redemption of {Redeeming} in progress",
                    nameof(MinimumBalanceAlgorithm), _options.Asset, change, _options.Asset, position.RedeemingAmount);

                return;
            }

            // check if there is enough for redemption
            if (change > position.FreeAmount)
            {
                _logger.LogError(
                    "{Type} {Name} cannot redeem {Change} {Asset} because there is only {Free} {Asset} free in savings",
                    nameof(MinimumBalanceAlgorithm), _options.Asset, change, _options.Asset, position.FreeAmount, _options.Asset);

                return;
            }

            await _trader
                .RedeemFlexibleProductAsync(_product.ProductId, change, FlexibleProductRedemptionType.Fast, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "{Type} {Name} redeemed {Change} {Asset}",
                nameof(MinimumBalanceAlgorithm), _options.Asset, change, _options.Asset);
        }
    }
}