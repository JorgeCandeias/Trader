using Microsoft.Extensions.DependencyInjection;
using Moq;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Commands;
using Outcompute.Trader.Trading.Commands.ClearOpenOrders;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class ClearOpenOrdersCommandTests
    {
        [Fact]
        public async Task Executes()
        {
            // arrange
            var symbol = Symbol.Empty with { Name = "ABCXYZ" };
            var side = OrderSide.Sell;
            var command = new ClearOpenOrdersCommand(symbol, side);
            var executor = Mock.Of<IAlgoCommandExecutor<ClearOpenOrdersCommand>>();
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