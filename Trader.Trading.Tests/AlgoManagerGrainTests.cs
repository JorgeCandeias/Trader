using Orleans;
using Orleans.TestingHost;
using Outcompute.Trader.Trading.Tests.Fixtures;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    [Collection(nameof(ClusterCollectionFixture))]
    public class AlgoManagerGrainTests
    {
        private readonly TestCluster _cluster;

        public AlgoManagerGrainTests(ClusterFixture cluster)
        {
            _cluster = cluster?.Cluster ?? throw new ArgumentNullException(nameof(cluster));
        }

        [Fact]
        public async Task GetsAlgos()
        {
            // arrange
            var grain = _cluster.GrainFactory.GetAlgoManagerGrain();

            // act
            var result = await grain.GetAlgosAsync().ConfigureAwait(false);

            // assert
            Assert.Equal(1, result.Count);

            var info = result.First();
            Assert.Equal("MyTestAlgo", info.Name);
            Assert.Equal("Test", info.Type);
            Assert.True(info.Enabled);
            Assert.Equal(TimeSpan.FromMinutes(1), info.MaxExecutionTime);
            Assert.Equal(TimeSpan.FromSeconds(10), info.TickDelay);
            Assert.False(info.TickEnabled);
        }
    }
}