using Orleans;
using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Binance.Tests.Fakes
{
    internal class FakeTradingServiceGrain : Grain, IFakeTradingServiceGrain
    {
        private readonly Dictionary<string, Dictionary<string, FlexibleProductPosition>> _positions = new();
        private readonly Dictionary<(string, FlexibleProductRedemptionType), LeftDailyRedemptionQuotaOnFlexibleProduct> _quotas = new();

        public Task<IReadOnlyCollection<FlexibleProductPosition>> GetFlexibleProductPositionsAsync(string asset)
        {
            if (asset is null) throw new ArgumentNullException(nameof(asset));

            var result = _positions.TryGetValue(asset, out var items)
                ? items.Values.ToImmutableList()
                : ImmutableList<FlexibleProductPosition>.Empty;

            return Task.FromResult<IReadOnlyCollection<FlexibleProductPosition>>(result);
        }

        public Task SetFlexibleProductPositionsAsync(IEnumerable<FlexibleProductPosition> items)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));

            foreach (var item in items)
            {
                if (!_positions.TryGetValue(item.Asset, out var lookup))
                {
                    _positions[item.Asset] = lookup = new Dictionary<string, FlexibleProductPosition>();
                }
                lookup[item.ProductId] = item;
            }

            return Task.CompletedTask;
        }

        public Task<LeftDailyRedemptionQuotaOnFlexibleProduct?> TryGetLeftDailyRedemptionQuotaOnFlexibleProductAsync(
            string productId,
            FlexibleProductRedemptionType type)
        {
            if (productId is null) throw new ArgumentNullException(nameof(productId));

            var result = _quotas.TryGetValue((productId, type), out var value) ? value : null;

            return Task.FromResult(result);
        }

        public Task SetLeftDailyRedemptionQuotaOnFlexibleProductAsync(string productId, FlexibleProductRedemptionType type, LeftDailyRedemptionQuotaOnFlexibleProduct item)
        {
            if (item is null) throw new ArgumentNullException(nameof(item));

            _quotas[(productId, type)] = item;

            return Task.CompletedTask;
        }
    }
}