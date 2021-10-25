using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.TestingHost;
using Outcompute.Trader.Trading.Binance.Tests.Fixtures;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.Binance.Tests
{
    [Collection(nameof(ClusterCollectionFixture))]
    public sealed class BinanceUserDataReadynessGrainTests: IDisposable
    {
        private readonly TestCluster _cluster;

        public BinanceUserDataReadynessGrainTests(ClusterFixture cluster)
        {
            _cluster = cluster?.Cluster ?? throw new ArgumentNullException(nameof(cluster));
        }

        [Fact]
        public async Task IsReadyAsync()
        {
            // act
            var result = await _cluster.GrainFactory.GetBinanceUserDataReadynessGrain().IsReadyAsync();

            // assert
            Assert.False(result);
        }

        public void Dispose()
        {
            //_cluster.Dispose();
        }
    }
}