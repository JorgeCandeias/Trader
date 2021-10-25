using Microsoft.Extensions.DependencyInjection;
using Moq;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Commands;
using Outcompute.Trader.Trading.Commands.CancelOrder;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class CancelOrderCommandTests
    {
        [Fact]
        public async Task Executes()
        {
            // arrange
            var symbol = Symbol.Empty with { Name = "ABCXYZ" };
            var orderId = 123;
            var command = new CancelOrderCommand(symbol, orderId);
            var executor = Mock.Of<IAlgoCommandExecutor<CancelOrderCommand>>();
            var provider = new ServiceCollection()
                .AddSingleton(executor)
                .BuildServiceProvider();
            var context = new AlgoContext(provider);

            // act
            await command.ExecuteAsync(context);

            // assert
            Assert.Equal(symbol, command.Symbol);
            Assert.Equal(orderId, command.OrderId);
            Mock.Get(executor).Verify(x => x.ExecuteAsync(context, command, CancellationToken.None));
        }
    }
}