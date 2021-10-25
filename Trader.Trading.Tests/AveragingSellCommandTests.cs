using Microsoft.Extensions.DependencyInjection;
using Moq;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Commands;
using Outcompute.Trader.Trading.Commands.AveragingSell;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class AveragingSellCommandTests
    {
        [Fact]
        public async Task Executes()
        {
            // arrange
            var symbol = Symbol.Empty;
            var orders = Array.Empty<OrderQueryResult>();
            var profitMultiplier = 1.10m;
            var redeemSavings = true;
            var executor = Mock.Of<IAlgoCommandExecutor<AveragingSellCommand>>();
            var provider = new ServiceCollection()
                .AddSingleton(executor)
                .BuildServiceProvider();
            var context = new AlgoContext(provider);
            var command = new AveragingSellCommand(symbol, orders, profitMultiplier, redeemSavings);

            // act
            await command.ExecuteAsync(context);

            // assert
            Assert.Equal(symbol, command.Symbol);
            Assert.Equal(orders, command.Orders);
            Assert.Equal(profitMultiplier, command.ProfitMultiplier);
            Assert.Equal(redeemSavings, command.RedeemSavings);
            Mock.Get(executor).Verify(x => x.ExecuteAsync(context, command, CancellationToken.None));
        }
    }
}