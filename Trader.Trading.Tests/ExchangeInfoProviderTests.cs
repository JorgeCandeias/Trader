using Microsoft.Extensions.DependencyInjection;
using Orleans.TestingHost;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.InMemory;
using Outcompute.Trader.Trading.Providers;
using Outcompute.Trader.Trading.Tests.Fixtures;
using System.Collections.Immutable;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    [Collection(nameof(ClusterCollectionFixture))]
    public class ExchangeInfoProviderTests
    {
        private readonly TestCluster _cluster;

        public ExchangeInfoProviderTests(ClusterFixture cluster)
        {
            _cluster = cluster?.Cluster ?? throw new ArgumentNullException(nameof(cluster));
        }

        [Fact]
        public async Task GetsExchangeInfo()
        {
            // arrange
            var symbol = Symbol.Empty with { Name = "ABCXYZ" };
            var info = ExchangeInfo.Empty with { Symbols = ImmutableList.Create(symbol) };
            await _cluster.ServiceProvider.GetRequiredService<IInMemoryTradingService>().SetExchangeInfoAsync(info);

            var provider = _cluster.ServiceProvider.GetRequiredService<IExchangeInfoProvider>();

            // act
            var result = provider.GetExchangeInfo();

            // assert
            Assert.NotNull(result);
            Assert.Contains(result.Symbols, x => x.Name == "ABCXYZ");
        }
    }
}