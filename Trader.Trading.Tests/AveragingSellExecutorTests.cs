using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Commands;
using Outcompute.Trader.Trading.Commands.AveragingSell;
using Outcompute.Trader.Trading.Commands.ClearOpenOrders;
using Outcompute.Trader.Trading.Commands.EnsureSingleOrder;
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
        public async Task ExecutesScenario()
        {
            // arrange
            var logger = NullLogger<AveragingSellExecutor>.Instance;

            var symbol = Symbol.Empty with
            {
                Name = "ABCXYZ",
                BaseAsset = "ABC",
                QuoteAsset = "XYZ",
                Filters = SymbolFilters.Empty with
                {
                    LotSize = LotSizeSymbolFilter.Empty with
                    {
                        StepSize = 1
                    },
                    PercentPrice = PercentPriceSymbolFilter.Empty with
                    {
                        MultiplierDown = 0.50m,
                        MultiplierUp = 2m
                    },
                    Price = PriceSymbolFilter.Empty with
                    {
                        TickSize = 10m
                    }
                }
            };

            var balance = Balance.Zero(symbol.BaseAsset) with { Free = 2000m };
            var balances = Mock.Of<IBalanceProvider>(x =>
                x.TryGetBalanceAsync(symbol.BaseAsset, CancellationToken.None) == Task.FromResult(balance));

            var position = SavingsPosition.Zero(symbol.BaseAsset);
            var savings = Mock.Of<ISavingsProvider>(x =>
                x.TryGetPositionAsync(symbol.BaseAsset, CancellationToken.None) == Task.FromResult(position));

            var ticker = new MiniTicker(symbol.Name, DateTime.Today, 1200, 0, 0, 0, 0, 0);
            var tickers = Mock.Of<ITickerProvider>(x =>
                x.TryGetTickerAsync(symbol.Name, CancellationToken.None) == Task.FromResult(ticker));

            var executor = new AveragingSellExecutor(logger, balances, savings, tickers);

            var clearOpenOrdersCommandExecutor = Mock.Of<IAlgoCommandExecutor<ClearOpenOrdersCommand>>();
            var singleOrderCommandExecutor = Mock.Of<IAlgoCommandExecutor<EnsureSingleOrderCommand>>();
            var provider = new ServiceCollection()
                .AddSingleton(clearOpenOrdersCommandExecutor)
                .AddSingleton(singleOrderCommandExecutor)
                .BuildServiceProvider();

            var context = new AlgoContext(provider);
            var orders = new[]
            {
                OrderQueryResult.Empty with { Symbol = symbol.Name, OrderId = 123, Price = 1100m, ExecutedQuantity = 1000m, Side = OrderSide.Buy },
                OrderQueryResult.Empty with { Symbol = symbol.Name, OrderId = 124, Price = 1000m, ExecutedQuantity = 1000m, Side = OrderSide.Buy },
            };
            var profitMultiplier = 1.10m;
            var redeemSavings = false;
            var command = new AveragingSellCommand(symbol, orders, profitMultiplier, redeemSavings);

            // act
            await executor.ExecuteAsync(context, command);

            // assert
            Mock.Get(singleOrderCommandExecutor)
                .Verify(x => x.ExecuteAsync(context, It.Is<EnsureSingleOrderCommand>(x =>
                    x.Symbol == symbol &&
                    x.Side == OrderSide.Sell &&
                    x.Type == OrderType.Limit &&
                    x.TimeInForce == TimeInForce.GoodTillCanceled &&
                    x.Quantity == 2000m &&
                    x.Price == 1160m &&
                    !x.RedeemSavings),
                    CancellationToken.None));
        }
    }
}