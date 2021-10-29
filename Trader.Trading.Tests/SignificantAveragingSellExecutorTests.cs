using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Commands;
using Outcompute.Trader.Trading.Commands.ClearOpenOrders;
using Outcompute.Trader.Trading.Commands.SignificantAveragingSell;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class SignificantAveragingSellExecutorTests
    {
        [Fact]
        public async Task ClearsSellOrdersOnNoBuyOrders()
        {
            // arrange
            var logger = NullLogger<SignificantAveragingSellExecutor>.Instance;
            var executor = new SignificantAveragingSellExecutor(logger);
            var clearOpenOrdersExecutor = Mock.Of<IAlgoCommandExecutor<ClearOpenOrdersCommand>>();
            var provider = new ServiceCollection()
                .AddSingleton(clearOpenOrdersExecutor)
                .BuildServiceProvider();
            var context = new AlgoContext(provider);
            var symbol = Symbol.Empty with
            {
                Name = "ABCXYZ",
                BaseAsset = "ABC",
                QuoteAsset = "XYZ"
            };
            var ticker = MiniTicker.Empty with
            {
                Symbol = symbol.Name,
                ClosePrice = 50000
            };
            var orders = ImmutableSortedSet<OrderQueryResult>.Empty;
            var minimumProfitRate = 1.01m;
            var redeemSavings = false;
            var command = new SignificantAveragingSellCommand(symbol, ticker, orders, minimumProfitRate, redeemSavings);

            // act
            await executor.ExecuteAsync(context, command, CancellationToken.None);

            // assert
            Mock.Get(clearOpenOrdersExecutor)
                .Verify(x => x.ExecuteAsync(context, It.Is<ClearOpenOrdersCommand>(x => x.Side == OrderSide.Sell && x.Symbol == symbol), CancellationToken.None));
        }
    }
}