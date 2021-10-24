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
    public class TradeProviderTests
    {
        private readonly TestCluster _cluster;

        public TradeProviderTests(ClusterFixture cluster)
        {
            _cluster = cluster?.Cluster ?? throw new ArgumentNullException(nameof(cluster));
        }

        [Fact]
        public async Task SetsAndGetsTrade()
        {
            // arrange
            var symbol = Guid.NewGuid().ToString();
            var tradeId = 123;
            var trade = AccountTrade.Empty with { Symbol = symbol, Id = tradeId };
            var provider = _cluster.ServiceProvider.GetRequiredService<ITradeProvider>();

            // act
            await provider.SetTradeAsync(trade);
            var result = await provider.TryGetTradeAsync(symbol, tradeId);

            // assert
            Assert.Equal(trade, result);
        }

        [Fact]
        public async Task SetsAndGetsTrades()
        {
            // arrange
            var symbol = Guid.NewGuid().ToString();
            var trade1 = AccountTrade.Empty with { Symbol = symbol, Id = 1 };
            var trade2 = AccountTrade.Empty with { Symbol = symbol, Id = 2 };
            var trade3 = AccountTrade.Empty with { Symbol = symbol, Id = 3 };
            var trades = new[] { trade1, trade2, trade3 };
            var provider = _cluster.ServiceProvider.GetRequiredService<ITradeProvider>();

            // act
            await provider.SetTradesAsync(symbol, trades);
            var results = await provider.GetTradesAsync(symbol);

            // assert
            Assert.Equal(3, results.Count);
            Assert.Contains(trade1, results);
            Assert.Contains(trade2, results);
            Assert.Contains(trade3, results);
        }
    }
}