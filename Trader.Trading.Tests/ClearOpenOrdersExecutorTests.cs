using Moq;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Commands.ClearOpenOrders;
using Outcompute.Trader.Trading.Providers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class ClearOpenOrdersExecutorTests
    {
        [Fact]
        public async Task Executes()
        {
            // arrange
            var symbol = Symbol.Empty with { Name = "ABCXYZ" };
            var orderId = 123;
            var side = OrderSide.Sell;
            var cancelled = CancelStandardOrderResult.Empty with { Symbol = symbol.Name, OrderId = orderId, Status = OrderStatus.Canceled, Side = side };
            var trader = Mock.Of<ITradingService>();
            Mock.Get(trader)
                .Setup(x => x.CancelOrderAsync(symbol.Name, orderId, CancellationToken.None))
                .Returns(Task.FromResult(cancelled))
                .Verifiable();
            var order = OrderQueryResult.Empty with { Symbol = symbol.Name, OrderId = orderId, Status = OrderStatus.New, Side = side };
            var orders = Mock.Of<IOrderProvider>();
            Mock.Get(orders)
                .Setup(x => x.GetOrdersAsync(symbol.Name, CancellationToken.None))
                .Returns(Task.FromResult<IReadOnlyList<OrderQueryResult>>(new[] { order }))
                .Verifiable();
            Mock.Get(orders)
                .Setup(x => x.SetOrderAsync(cancelled, CancellationToken.None))
                .Verifiable();
            var executor = new ClearOpenOrdersExecutor(trader, orders);
            var context = AlgoContext.Empty;
            var command = new ClearOpenOrdersCommand(symbol, side);

            // act
            await executor.ExecuteAsync(context, command);

            // assert
            Mock.Get(orders).VerifyAll();
            Mock.Get(trader).VerifyAll();
        }
    }
}