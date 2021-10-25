using Orleans;
using Orleans.TestingHost;
using Outcompute.Trader.Trading.Binance.Tests.Fixtures;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.Binance.Tests
{
    [Collection(nameof(ClusterCollectionFixture))]
    public class BinanceUserDataGrainTests
    {
        private readonly TestCluster _cluster;

        public BinanceUserDataGrainTests(ClusterFixture cluster)
        {
            _cluster = cluster?.Cluster ?? throw new ArgumentNullException(nameof(cluster));
        }

        [Fact]
        public async Task Pings()
        {
            // act
            await _cluster.GrainFactory.GetBinanceUserDataGrain().PingAsync();

            // assert
            Assert.True(true);
        }
    }
}