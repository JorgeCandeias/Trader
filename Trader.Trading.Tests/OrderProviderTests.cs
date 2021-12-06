using Outcompute.Trader.Models.Hosting;
using Outcompute.Trader.Trading.Providers.Orders;

namespace Outcompute.Trader.Trading.Tests
{
    public class OrderProviderTests
    {
        private readonly IMapper _mapper = new MapperConfiguration(options => { options.AddProfile<ModelsProfile>(); }).CreateMapper();

        [Fact]
        public async Task SetsOrder()
        {
            // arrange
            var order = OrderQueryResult.Empty with { Symbol = "ABCXYZ", OrderId = 123 };
            var factory = Mock.Of<IGrainFactory>();
            Mock.Get(factory)
                .Setup(x => x.GetGrain<IOrderProviderReplicaGrain>(order.Symbol, null).SetOrderAsync(order))
                .Verifiable();
            var provider = new OrderProvider(factory, _mapper);

            // act
            await provider.SetOrderAsync(order);

            // assert
            Mock.Get(factory).VerifyAll();
        }

        [Fact]
        public async Task GetsOrder()
        {
            // arrange
            var order = OrderQueryResult.Empty with { Symbol = "ABCXYZ", OrderId = 123 };
            var factory = Mock.Of<IGrainFactory>();
            Mock.Get(factory)
                .Setup(x => x.GetGrain<IOrderProviderReplicaGrain>(order.Symbol, null).TryGetOrderAsync(order.OrderId))
                .ReturnsAsync(order)
                .Verifiable();
            var provider = new OrderProvider(factory, _mapper);

            // act
            var result = await provider.TryGetOrderAsync(order.Symbol, order.OrderId);

            // assert
            Assert.Same(order, result);
            Mock.Get(factory).VerifyAll();
        }

        [Fact]
        public async Task SetsOrders()
        {
            // arrange
            var symbol = "ABCXYZ";
            var orders = new[]
            {
                OrderQueryResult.Empty with { Symbol = symbol, OrderId = 1 },
                OrderQueryResult.Empty with { Symbol = symbol, OrderId = 2 },
                OrderQueryResult.Empty with { Symbol = symbol, OrderId = 3 }
            };

            var factory = Mock.Of<IGrainFactory>();
            Mock.Get(factory)
                .Setup(x => x.GetGrain<IOrderProviderReplicaGrain>(symbol, null).SetOrdersAsync(orders))
                .Verifiable();
            var provider = new OrderProvider(factory, _mapper);

            // act
            await provider.SetOrdersAsync(symbol, orders);

            // assert
            Mock.Get(factory).VerifyAll();
        }

        [Fact]
        public async Task GetsOrders()
        {
            // arrange
            var symbol = "ABCXYZ";
            var orders = ImmutableSortedSet.Create(OrderQueryResult.KeyComparer,
                OrderQueryResult.Empty with { Symbol = symbol, OrderId = 1 },
                OrderQueryResult.Empty with { Symbol = symbol, OrderId = 2 },
                OrderQueryResult.Empty with { Symbol = symbol, OrderId = 3 });

            var factory = Mock.Of<IGrainFactory>();
            Mock.Get(factory)
                .Setup(x => x.GetGrain<IOrderProviderReplicaGrain>(symbol, null).GetOrdersAsync())
                .ReturnsAsync(orders)
                .Verifiable();
            var provider = new OrderProvider(factory, _mapper);

            // act
            var result = await provider.GetOrdersAsync(symbol);

            // assert
            Assert.Same(orders, result);
            Mock.Get(factory).VerifyAll();
        }
    }
}