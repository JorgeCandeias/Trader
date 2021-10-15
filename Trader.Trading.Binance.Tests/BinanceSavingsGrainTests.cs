using Orleans;
using Orleans.TestingHost;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Binance.Tests.Fixtures;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.Binance.Tests
{
    [Collection(nameof(ClusterCollectionFixture))]
    public class BinanceSavingsGrainTests
    {
        private readonly TestCluster _cluster;

        public BinanceSavingsGrainTests(ClusterFixture cluster)
        {
            _cluster = cluster?.Cluster ?? throw new ArgumentNullException(nameof(cluster));
        }

        [Fact]
        public async Task GetsFlexibleProductPosition()
        {
            // arrange
            var asset = Guid.NewGuid().ToString();

            var positions = Enumerable
                .Range(1, 3)
                .Select(x => new FlexibleProductPosition(asset, $"P{x}", $"Product{x}", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, true))
                .ToImmutableList();

            var quotas = Enumerable
                .Range(1, 3)
                .Select(x => (ProductId: $"P{x}", Quota: new LeftDailyRedemptionQuotaOnFlexibleProduct(asset, 1000000m, 1000000m, x)))
                .ToImmutableList();

            await _cluster.GrainFactory
                .GetFakeTradingServiceGrain()
                .SetFlexibleProductPositionsAsync(positions);

            foreach (var (productId, quota) in quotas)
            {
                await _cluster.GrainFactory
                    .GetFakeTradingServiceGrain()
                    .SetLeftDailyRedemptionQuotaOnFlexibleProductAsync(productId, FlexibleProductRedemptionType.Fast, quota);
            }

            // act
            var result = await _cluster.GrainFactory
                .GetBinanceSavingsGrain(asset)
                .GetFlexibleProductPositionsAsync();

            Assert.Equal(3, result.Count);
            Assert.Contains(positions, x => x.Asset == asset && x.ProductId == "P1");
            Assert.Contains(positions, x => x.Asset == asset && x.ProductId == "P2");
            Assert.Contains(positions, x => x.Asset == asset && x.ProductId == "P3");
        }
    }
}