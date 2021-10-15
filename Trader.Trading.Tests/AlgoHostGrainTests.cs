using Orleans;
using Orleans.TestingHost;
using Outcompute.Trader.Trading.Tests.Fixtures;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    [Collection(nameof(ClusterCollectionFixture))]
    public class AlgoHostGrainTests
    {
        private readonly TestCluster _cluster;

        public AlgoHostGrainTests(ClusterFixture cluster)
        {
            _cluster = cluster?.Cluster ?? throw new ArgumentNullException(nameof(cluster));
        }

        [Fact]
        public async Task Pings()
        {
            // arrange
            var grain = _cluster.GrainFactory.GetAlgoHostGrain("MyTestAlgo");

            // act
            await grain.PingAsync().ConfigureAwait(false);

            // assert
            Assert.True(true);
        }
    }
}