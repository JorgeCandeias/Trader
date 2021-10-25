using Microsoft.Extensions.DependencyInjection;
using Moq;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Commands;
using Outcompute.Trader.Trading.Commands.CreateOrder;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class CreateOrderCommandTests
    {
        [Fact]
        public async Task Executes()
        {
            // arrange
            var symbol = Symbol.Empty with { Name = "ABCXYZ" };
            var type = OrderType.Limit;
            var side = OrderSide.Buy;
            var timeInForce = TimeInForce.GoodTillCanceled;
            var quantity = 123m;
            var price = 234m;
            var tag = "ZZZ";
            var command = new CreateOrderCommand(symbol, type, side, timeInForce, quantity, price, tag);
            var executor = Mock.Of<IAlgoCommandExecutor<CreateOrderCommand>>();
            var provider = new ServiceCollection()
                .AddSingleton(executor)
                .BuildServiceProvider();
            var context = new AlgoContext(provider);

            // act
            await command.ExecuteAsync(context);

            // assert
            Assert.Equal(symbol, command.Symbol);
            Assert.Equal(type, command.Type);
            Assert.Equal(side, command.Side);
            Assert.Equal(timeInForce, command.TimeInForce);
            Assert.Equal(quantity, command.Quantity);
            Assert.Equal(price, command.Price);
            Assert.Equal(tag, command.Tag);
            Mock.Get(executor).Verify(x => x.ExecuteAsync(context, command, CancellationToken.None));
        }
    }
}