using Orleans;
using Orleans.TestingHost;
using Outcompute.Trader.Trading.Binance.Tests.Fixtures;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.Binance.Tests
{
    [Collection(nameof(ClusterCollectionFixture))]
    public class BinanceMarketDataReadynessGrainTests
    {
        private readonly TestCluster _cluster;

        public BinanceMarketDataReadynessGrainTests(ClusterFixture cluster)
        {
            _cluster = cluster?.Cluster ?? throw new ArgumentNullException(nameof(cluster));
        }

        [Fact]
        public async Task IsReadySignalsFalse()
        {
            // arrange

            // act
            var result = await _cluster.GrainFactory
                .GetBinanceMarketDataReadynessGrain()
                .IsReadyAsync();

            // assert
            Assert.False(result);
        }
    }
}