using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Commands;
using Outcompute.Trader.Trading.Commands.AveragingSell;
using Outcompute.Trader.Trading.Commands.ClearOpenOrders;
using Outcompute.Trader.Trading.Providers;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class AveragingSellExecutorTests
    {
        [Fact]
        public async Task ClearsOpenSellOrdersOnNoBuyOrders()
        {
            // arrange
            var logger = NullLogger<AveragingSellExecutor>.Instance;

            var symbol = Symbol.Empty with { Name = "ABCXYZ", BaseAsset = "ABC", QuoteAsset = "XYZ" };

            var balance = Balance.Zero(symbol.BaseAsset);
            var balances = Mock.Of<IBalanceProvider>(x =>
                x.TryGetBalanceAsync(symbol.BaseAsset, CancellationToken.None) == Task.FromResult(balance));

            var position = SavingsPosition.Zero(symbol.BaseAsset);
            var savings = Mock.Of<ISavingsProvider>(x =>
                x.TryGetPositionAsync(symbol.BaseAsset, CancellationToken.None) == Task.FromResult(position));

            var ticker = new MiniTicker(symbol.Name, DateTime.Today, 0, 0, 0, 0, 0, 0);
            var tickers = Mock.Of<ITickerProvider>(x =>
                x.TryGetTickerAsync(symbol.Name, CancellationToken.None) == Task.FromResult(ticker));

            var executor = new AveragingSellExecutor(logger, balances, savings, tickers);

            var clearOpenOrdersCommandExecutor = Mock.Of<IAlgoCommandExecutor<ClearOpenOrdersCommand>>();
            var provider = new ServiceCollection()
                .AddSingleton(clearOpenOrdersCommandExecutor)
                .BuildServiceProvider();

            var context = new AlgoContext(provider);
            var orders = Array.Empty<OrderQueryResult>();
            var profitMultiplier = 1.10m;
            var redeemSavings = false;
            var command = new AveragingSellCommand(symbol, orders, profitMultiplier, redeemSavings);

            // act
            await executor.ExecuteAsync(context, command);

            // assert
            Mock.Get(clearOpenOrdersCommandExecutor)
                .Verify(x => x.ExecuteAsync(context, It.Is<ClearOpenOrdersCommand>(x => x.Symbol == symbol && x.Side == OrderSide.Sell), CancellationToken.None));
        }
    }
}