using Orleans;
using Orleans.TestingHost;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Tests.Fixtures;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    [Collection(nameof(ClusterCollectionFixture))]
    public class OrderProviderGrainTests
    {
        private readonly TestCluster _cluster;

        public OrderProviderGrainTests(ClusterFixture cluster)
        {
            _cluster = cluster?.Cluster ?? throw new ArgumentNullException(nameof(cluster));
        }

        [Fact]
        public async Task SetGetOrdersAsync()
        {
            // arrange
            var symbol = Guid.NewGuid().ToString();
            var orders = new[]
            {
                OrderQueryResult.Empty with { Symbol = symbol, OrderId = 1001 },
                OrderQueryResult.Empty with { Symbol = symbol, OrderId = 1002 },
                OrderQueryResult.Empty with { Symbol = symbol, OrderId = 1003 }
            };
            var grain = _cluster.GrainFactory.GetOrderProviderGrain(symbol);

            // act
            await grain.SetOrdersAsync(orders);
            var result = await grain.GetOrdersAsync();

            // assert
            Assert.NotEqual(Guid.Empty, result.Version);
            Assert.Equal(3, result.MaxSerial);
            Assert.Contains(result.Orders, x => x.OrderId == 1001);
            Assert.Contains(result.Orders, x => x.OrderId == 1002);
            Assert.Contains(result.Orders, x => x.OrderId == 1003);
        }
    }
}