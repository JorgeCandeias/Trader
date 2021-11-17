using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Commands;
using Outcompute.Trader.Trading.Commands.AveragingSell;
using Outcompute.Trader.Trading.Commands.CancelOpenOrders;
using Outcompute.Trader.Trading.Commands.EnsureSingleOrder;
using System.Collections.Immutable;
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

            var orders = ImmutableSortedSet.Create(OrderQueryResult.KeyComparer, new[]
            {
                OrderQueryResult.Empty with { Symbol = symbol.Name, OrderId = 123, Price = 1100m, ExecutedQuantity = 1000m, Side = OrderSide.Buy },
                OrderQueryResult.Empty with { Symbol = symbol.Name, OrderId = 124, Price = 1000m, ExecutedQuantity = 1000m, Side = OrderSide.Buy },
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
                SpotBalances =
                {
                    [symbol.Name] = SymbolSpotBalances.Empty with
                    {
                        BaseAsset = Balance.Zero(symbol.BaseAsset) with { Free = 2000M }
                    }
                },
                Savings = { [symbol.Name] = SymbolSavingsBalances.Empty with
                {
                    Symbol = symbol.Name,
                    BaseAsset = SavingsBalance.Zero(symbol.BaseAsset),
                    QuoteAsset = SavingsBalance.Zero(symbol.QuoteAsset)
                } },
                Tickers = { [symbol.Name] = new MiniTicker(symbol.Name, DateTime.Today, 1200, 0, 0, 0, 0, 0) },
                AutoPositions = { [symbol.Name] = AutoPosition.Empty with { Orders = orders } }
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