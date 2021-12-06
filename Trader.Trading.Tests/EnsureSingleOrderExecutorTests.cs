using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Commands;
using Outcompute.Trader.Trading.Commands.CancelOrder;
using Outcompute.Trader.Trading.Commands.CreateOrder;
using Outcompute.Trader.Trading.Commands.EnsureSingleOrder;
using Outcompute.Trader.Trading.Providers;

namespace Outcompute.Trader.Trading.Tests
{
    public class EnsureSingleOrderExecutorTests
    {
        [Fact]
        public async Task RedeemsSavings()
        {
            // arrange
            var logger = NullLogger<EnsureSingleOrderExecutor>.Instance;
            var symbol = Symbol.Empty with { Name = "ABCXYZ", BaseAsset = "ABC", QuoteAsset = "XYZ" };

            var side = OrderSide.Buy;

            var existing = new[]
            {
                OrderQueryResult.Empty with { Symbol = symbol.Name, OrderId = 123, Status = OrderStatus.New, Side = side }
            };

            var orders = Mock.Of<IOrderProvider>();

            var executor = new EnsureSingleOrderExecutor(logger);

            var cancelOrderExecutor = Mock.Of<IAlgoCommandExecutor<CancelOrderCommand>>();

            var createOrderExecutor = Mock.Of<IAlgoCommandExecutor<CreateOrderCommand>>();

            var provider = new ServiceCollection()
                .AddSingleton(cancelOrderExecutor)
                .AddSingleton(createOrderExecutor)
                .BuildServiceProvider();

            var context = new AlgoContext("Algo1", provider);
            context.Data[symbol.Name].Orders.Open = ImmutableSortedSet.Create(OrderQueryResult.KeyComparer, existing);

            var type = OrderType.Limit;
            var timeInForce = TimeInForce.GoodTillCanceled;
            var quantity = 1000m;
            var price = 1234m;
            var command = new EnsureSingleOrderCommand(symbol, side, type, timeInForce, quantity, null, price, null, null);

            // act
            await executor.ExecuteAsync(context, command);

            // assert
            Mock.Get(orders).VerifyAll();
            Mock.Get(cancelOrderExecutor).Verify(x => x.ExecuteAsync(context, It.Is<CancelOrderCommand>(x => x.Symbol == symbol && x.OrderId == 123), CancellationToken.None));
        }
    }
}