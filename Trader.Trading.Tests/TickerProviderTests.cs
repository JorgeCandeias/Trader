using Microsoft.Extensions.DependencyInjection;
using Orleans.TestingHost;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers;
using Outcompute.Trader.Trading.Tests.Fixtures;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    [Collection(nameof(ClusterCollectionFixture))]
    public class TickerProviderTests
    {
        private readonly TestCluster _cluster;

        public TickerProviderTests(ClusterFixture cluster)
        {
            _cluster = cluster?.Cluster ?? throw new ArgumentNullException(nameof(cluster));
        }

        [Fact]
        public async Task SetsAndGetsTicker()
        {
            // arrange
            var symbol = Guid.NewGuid().ToString();
            var ticker = MiniTicker.Empty with { Symbol = symbol };
            var provider = _cluster.ServiceProvider.GetRequiredService<ITickerProvider>();

            // act
            await provider.SetTickerAsync(ticker);
            var result = await provider.TryGetTickerAsync(symbol);

            // assert
            Assert.Equal(ticker, result);
        }
    }
}