using Microsoft.Extensions.DependencyInjection;
using Moq;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Commands;
using Outcompute.Trader.Trading.Commands.CancelOpenOrders;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class CancelOpenOrdersCommandTests
    {
        [Fact]
        public async Task Executes()
        {
            // arrange
            var symbol = Symbol.Empty with { Name = "ABCXYZ" };
            var side = OrderSide.Sell;
            var command = new CancelOpenOrdersCommand(symbol, side);
            var executor = Mock.Of<IAlgoCommandExecutor<CancelOpenOrdersCommand>>();
            var provider = new ServiceCollection()
                .AddSingleton(executor)
                .BuildServiceProvider();
            var context = new AlgoContext("Algo1", provider);

            // act
            await command.ExecuteAsync(context);

            // assert
            Mock.Get(executor).Verify(x => x.ExecuteAsync(context, command, CancellationToken.None));
        }
    }
}