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
    public class OrderProviderTests
    {
        private readonly TestCluster _cluster;

        public OrderProviderTests(ClusterFixture cluster)
        {
            _cluster = cluster?.Cluster ?? throw new ArgumentNullException(nameof(cluster));
        }

        [Fact]
        public async Task SetsAndGetsOrder()
        {
            // arrange
            var symbol = Guid.NewGuid().ToString();
            var orderId = 123;
            var order = OrderQueryResult.Empty with { Symbol = symbol, OrderId = orderId };
            var provider = _cluster.ServiceProvider.GetRequiredService<IOrderProvider>();

            // act
            await provider.SetOrderAsync(order);
            var result = await provider.TryGetOrderAsync(symbol, orderId);

            // assert
            Assert.Equal(order, result);
        }

        [Fact]
        public async Task SetsAndGetsOrders()
        {
            // arrange
            var symbol = Guid.NewGuid().ToString();
            var order1 = OrderQueryResult.Empty with { Symbol = symbol, OrderId = 1 };
            var order2 = OrderQueryResult.Empty with { Symbol = symbol, OrderId = 2 };
            var order3 = OrderQueryResult.Empty with { Symbol = symbol, OrderId = 3 };
            var orders = new[] { order1, order2, order3 };
            var provider = _cluster.ServiceProvider.GetRequiredService<IOrderProvider>();

            // act
            await provider.SetOrdersAsync(symbol, orders);
            var results = await provider.GetOrdersAsync(symbol);

            // assert
            Assert.Equal(3, results.Count);
            Assert.Contains(order1, results);
            Assert.Contains(order2, results);
            Assert.Contains(order3, results);
        }
    }
}