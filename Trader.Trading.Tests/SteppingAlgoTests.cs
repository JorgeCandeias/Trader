using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Algorithms.Stepping;
using Outcompute.Trader.Trading.Commands.ClearOpenOrders;
using Outcompute.Trader.Trading.Providers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class SteppingAlgoTests
    {
        [Fact]
        public async Task ReturnsClearOpenOrdersOnDisabledOpening()
        {
            // arrange
            var name = "MyAlgo";
            var logger = NullLogger<SteppingAlgo>.Instance;
            var options = Mock.Of<IOptionsMonitor<SteppingAlgoOptions>>(
                x => !x.Get(name).IsOpeningEnabled);

            var symbol = Symbol.Empty with
            {
                Name = "ABCXYZ",
                BaseAsset = "ABC",
                QuoteAsset = "XYZ"
            };

            var orderProvider = Mock.Of<IOrderProvider>();
            Mock.Get(orderProvider)
                .Setup(x => x.GetOrdersAsync(symbol.Name, CancellationToken.None))
                .Returns(Task.FromResult<IReadOnlyList<OrderQueryResult>>(ImmutableList<OrderQueryResult>.Empty))
                .Verifiable();

            var algo = new SteppingAlgo(logger, options, orderProvider);

            var provider = new ServiceCollection()
                .BuildServiceProvider();

            algo.Context = new AlgoContext(provider)
            {
                Name = name,
                Symbol = symbol
            };

            // act
            var result = await algo.GoAsync();

            // arrange
            Mock.Get(orderProvider).VerifyAll();

            var command = Assert.IsType<ClearOpenOrdersCommand>(result);
            Assert.Equal(symbol, command.Symbol);
            Assert.Equal(OrderSide.Buy, command.Side);
        }
    }
}