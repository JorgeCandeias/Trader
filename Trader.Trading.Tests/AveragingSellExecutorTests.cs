using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Algorithms.Positions;
using Outcompute.Trader.Trading.Commands;
using Outcompute.Trader.Trading.Commands.AveragingSell;
using Outcompute.Trader.Trading.Commands.CancelOpenOrders;
using Outcompute.Trader.Trading.Commands.EnsureSingleOrder;
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

            var positions = new PositionCollection(new[]
            {
                Position.Empty with { Symbol = symbol, OrderId = 123, Price = 1100m, Quantity = 1000m },
                Position.Empty with { Symbol = symbol, OrderId = 124, Price = 1000m, Quantity = 1000m },
            });

            var executor = new AveragingSellExecutor(logger);

            var clearOpenOrdersCommandExecutor = Mock.Of<IAlgoCommandExecutor<CancelOpenOrdersCommand>>();
            var singleOrderCommandExecutor = Mock.Of<IAlgoCommandExecutor<EnsureSingleOrderCommand>>();
            var provider = new ServiceCollection()
                .AddSingleton(clearOpenOrdersCommandExecutor)
                .AddSingleton(singleOrderCommandExecutor)
                .BuildServiceProvider();

            var context = new AlgoContext("Algo1", provider)
            {
                Symbol = symbol,
                Data =
                {
                    new SymbolData(symbol.Name)
                    {
                        AutoPosition = AutoPosition.Empty with { Positions = positions },
                        Ticker = new MiniTicker(symbol.Name, DateTime.Today, 1200, 0, 0, 0, 0, 0),
                        Spot =
                        {
                            BaseAsset = Balance.Zero(symbol.BaseAsset) with { Free = 2000M }
                        },
                        Savings =
                        {
                            BaseAsset = SavingsBalance.Zero(symbol.BaseAsset),
                            QuoteAsset = SavingsBalance.Zero(symbol.QuoteAsset)
                        }
                    }
                }
            };

            var profitMultiplier = 1.10m;
            var redeemSavings = false;
            var redeemSwapPool = false;
            var command = new AveragingSellCommand(symbol, profitMultiplier, redeemSavings, redeemSwapPool);

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