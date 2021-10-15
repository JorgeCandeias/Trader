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
                .Select(x => new FlexibleProductPosition(0, asset, 0, true, 0, 0, 0, 0, $"P{x}", $"Product{x}", 0, 0, 0, 0))
                .ToImmutableList();

            var quotas = Enumerable
                .Range(1, 3)
                .Select(x => (ProductId: $"P{x}", Quota: new LeftDailyRedemptionQuotaOnFlexibleProduct(asset, 1000000m, 1000000m, x)))
                .ToImmutableList();

            await _cluster.GrainFactory
                .GetFakeTradingServiceGrain()
                .SetFlexibleProductPositionsAsync(positions);

            foreach (var item in quotas)
            {
                await _cluster.GrainFactory
                    .GetFakeTradingServiceGrain()
                    .SetLeftDailyRedemptionQuotaOnFlexibleProductAsync(item.ProductId, FlexibleProductRedemptionType.Fast, item.Quota);
            }

            // act
            var result = await _cluster.GrainFactory
                .GetBinanceSavingsGrain(asset)
                .GetFlexibleProductPositionAsync();

            Assert.Equal(3, result.Count);
            Assert.Contains(positions, x => x.Asset == asset && x.ProductId == "P1");
            Assert.Contains(positions, x => x.Asset == asset && x.ProductId == "P2");
            Assert.Contains(positions, x => x.Asset == asset && x.ProductId == "P3");
        }
    }
}