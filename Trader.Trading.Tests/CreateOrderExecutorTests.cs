using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Commands.CreateOrder;
using Outcompute.Trader.Trading.Providers;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class CreateOrderExecutorTests
    {
        [Fact]
        public async Task Executes()
        {
            // arrange
            var logger = NullLogger<CreateOrderExecutor>.Instance;
            var symbol = Symbol.Empty with { Name = "ABCXYZ" };
            var side = OrderSide.Buy;
            var type = OrderType.Limit;
            var timeInForce = TimeInForce.GoodTillCanceled;
            var quantity = 123m;
            var price = 234m;
            var tag = "ZZZ";
            var orderId = 123456;

            var created = OrderResult.Empty with { Symbol = symbol.Name, OrderId = orderId };

            var trader = Mock.Of<ITradingService>();
            Mock.Get(trader)
                .Setup(x => x.CreateOrderAsync(symbol.Name, side, type, timeInForce, quantity, null, price, tag, null, null, CancellationToken.None))
                .Returns(Task.FromResult(created))
                .Verifiable();

            var orders = Mock.Of<IOrderProvider>();
            Mock.Get(orders)
                .Setup(x => x.SetOrderAsync(created, 0, 0, 0, CancellationToken.None))
                .Verifiable();

            var executor = new CreateOrderExecutor(logger, trader, orders);
            var context = AlgoContext.Empty;
            var command = new CreateOrderCommand(symbol, type, side, timeInForce, quantity, price, null, tag);

            // act
            await executor.ExecuteAsync(context, command);

            // assert
            Mock.Get(trader).VerifyAll();
            Mock.Get(orders).VerifyAll();
        }
    }
}