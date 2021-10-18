using Orleans;
using Outcompute.Trader.Trading.Tests.Fixtures;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    [Collection(nameof(ClusterCollectionFixture))]
    public class OrderProviderGrainTests
    {
        private readonly IGrainFactory _factory;

        public OrderProviderGrainTests(ClusterFixture cluster)
        {
            _factory = cluster?.Cluster.GrainFactory ?? throw new ArgumentNullException(nameof(cluster));
        }

        [Fact]
        public async Task GetOrdersAsync()
        {
            // arrange
            var symbol = Guid.NewGuid().ToString();

            // act
            var result = await _factory.GetOrderProviderGrain(symbol).GetOrdersAsync();

            // assert
            Assert.NotEqual(Guid.Empty, result.Version);
            Assert.Equal(3, result.MaxSerial);
            Assert.Collection(result.Orders,
                x =>
                {
                    Assert.Equal(symbol, x.Symbol);
                    Assert.Equal(1001, x.OrderId);
                },
                x =>
                {
                    Assert.Equal(symbol, x.Symbol);
                    Assert.Equal(1002, x.OrderId);
                },
                x =>
                {
                    Assert.Equal(symbol, x.Symbol);
                    Assert.Equal(1003, x.OrderId);
                });
        }
    }
}