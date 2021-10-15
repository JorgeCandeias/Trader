using Orleans;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Binance.Providers.Savings
{
    internal class BinanceSavingsProvider : ISavingsProvider
    {
        private readonly IGrainFactory _factory;

        public BinanceSavingsProvider(IGrainFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public ValueTask<IReadOnlyCollection<FlexibleProductPosition>> GetFlexibleProductPositionAsync(string asset, CancellationToken cancellationToken = default)
        {
            return _factory.GetBinanceSavingsGrain(asset).GetFlexibleProductPositionsAsync();
        }

        public ValueTask<FlexibleProductPosition?> TryGetFirstFlexibleProductPositionAsync(string asset, CancellationToken cancellation = default)
        {
            return _factory.GetBinanceSavingsGrain(asset).TryGetFirstFlexibleProductPositionAsync();
        }

        public ValueTask<LeftDailyRedemptionQuotaOnFlexibleProduct?> TryGetLeftDailyRedemptionQuotaOnFlexibleProductAsync(string asset, string productId, FlexibleProductRedemptionType type, CancellationToken cancellationToken = default)
        {
            return _factory.GetBinanceSavingsGrain(asset).TryGetLeftDailyRedemptionQuotaOnFlexibleProductAsync(productId, type);
        }

        public ValueTask RedeemFlexibleProductAsync(string asset, string productId, decimal amount, FlexibleProductRedemptionType type, CancellationToken cancellationToken = default)
        {
            return _factory.GetBinanceSavingsGrain(asset).RedeemFlexibleProductAsync(productId, amount, type);
        }
    }
}