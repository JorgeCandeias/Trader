using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Commands;
using Outcompute.Trader.Trading.Commands.CancelOpenOrders;
using Outcompute.Trader.Trading.Commands.EnsureSingleOrder;
using Outcompute.Trader.Trading.Commands.SignificantAveragingSell;
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
            var clearOpenOrdersExecutor = Mock.Of<IAlgoCommandExecutor<CancelOpenOrdersCommand>>();
            var provider = new ServiceCollection()
                .AddSingleton(clearOpenOrdersExecutor)
                .BuildServiceProvider();
            var context = new AlgoContext("Algo1", provider);
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
            var orders = Array.Empty<OrderQueryResult>();
            var minimumProfitRate = 1.01m;
            var redeemSavings = false;
            var redeemSwapPool = true;
            var command = new SignificantAveragingSellCommand(symbol, ticker, orders, minimumProfitRate, redeemSavings, redeemSwapPool);

            // act
            await executor.ExecuteAsync(context, command, CancellationToken.None);

            // assert
            Mock.Get(clearOpenOrdersExecutor)
                .Verify(x => x.ExecuteAsync(context, It.Is<CancelOpenOrdersCommand>(x => x.Side == OrderSide.Sell && x.Symbol == symbol), CancellationToken.None));
        }

        [Fact]
        public async Task ClearsSellOrdersOnNoSellableBuyOrders()
        {
            // arrange
            var logger = NullLogger<SignificantAveragingSellExecutor>.Instance;
            var executor = new SignificantAveragingSellExecutor(logger);
            var clearOpenOrdersExecutor = Mock.Of<IAlgoCommandExecutor<CancelOpenOrdersCommand>>();
            var provider = new ServiceCollection()
                .AddSingleton(clearOpenOrdersExecutor)
                .BuildServiceProvider();
            var context = new AlgoContext("Algo1", provider);
            var symbol = Symbol.Empty with
            {
                Name = "ABCXYZ",
                BaseAsset = "ABC",
                QuoteAsset = "XYZ",
                Filters = SymbolFilters.Empty with
                {
                    Price = PriceSymbolFilter.Empty with
                    {
                        TickSize = 1m
                    }
                }
            };
            var ticker = MiniTicker.Empty with
            {
                Symbol = symbol.Name,
                ClosePrice = 50000
            };
            var orders = new[]
            {
                OrderQueryResult.Empty with
                {
                    Symbol = symbol.Name,
                    Side = OrderSide.Buy,
                    ExecutedQuantity = 100m,
                    Price = 60000m
                }
            };
            var minimumProfitRate = 1.01m;
            var redeemSavings = false;
            var redeemSwapPool = true;
            var command = new SignificantAveragingSellCommand(symbol, ticker, orders, minimumProfitRate, redeemSavings, redeemSwapPool);

            // act
            await executor.ExecuteAsync(context, command, CancellationToken.None);

            // assert
            Mock.Get(clearOpenOrdersExecutor)
                .Verify(x => x.ExecuteAsync(context, It.Is<CancelOpenOrdersCommand>(x => x.Side == OrderSide.Sell && x.Symbol == symbol), CancellationToken.None));
        }

        [Fact]
        public async Task ClearsSellOrdersOnSellableQuantityUnderMinLotSize()
        {
            // arrange
            var logger = NullLogger<SignificantAveragingSellExecutor>.Instance;
            var executor = new SignificantAveragingSellExecutor(logger);
            var clearOpenOrdersExecutor = Mock.Of<IAlgoCommandExecutor<CancelOpenOrdersCommand>>();
            var provider = new ServiceCollection()
                .AddSingleton(clearOpenOrdersExecutor)
                .BuildServiceProvider();
            var context = new AlgoContext("Algo1", provider);
            var symbol = Symbol.Empty with
            {
                Name = "ABCXYZ",
                BaseAsset = "ABC",
                QuoteAsset = "XYZ",
                Filters = SymbolFilters.Empty with
                {
                    Price = PriceSymbolFilter.Empty with
                    {
                        TickSize = 1m
                    },
                    LotSize = LotSizeSymbolFilter.Empty with
                    {
                        StepSize = 1m,
                        MinQuantity = 1000m
                    }
                }
            };
            var ticker = MiniTicker.Empty with
            {
                Symbol = symbol.Name,
                ClosePrice = 50000
            };
            var orders = new[]
            {
                OrderQueryResult.Empty with
                {
                    Symbol = symbol.Name,
                    Side = OrderSide.Buy,
                    ExecutedQuantity = 100m,
                    Price = 50000m
                },
                OrderQueryResult.Empty with
                {
                    Symbol = symbol.Name,
                    Side = OrderSide.Buy,
                    ExecutedQuantity = 100m,
                    Price = 40000m
                }
            };
            var minimumProfitRate = 1.01m;
            var redeemSavings = false;
            var redeemSwapPool = true;
            var command = new SignificantAveragingSellCommand(symbol, ticker, orders, minimumProfitRate, redeemSavings, redeemSwapPool);

            // act
            await executor.ExecuteAsync(context, command, CancellationToken.None);

            // assert
            Mock.Get(clearOpenOrdersExecutor)
                .Verify(x => x.ExecuteAsync(context, It.Is<CancelOpenOrdersCommand>(x => x.Side == OrderSide.Sell && x.Symbol == symbol), CancellationToken.None));
        }

        [Fact]
        public async Task ClearsSellOrdersOnSellableQuantityUnderMinNotional()
        {
            // arrange
            var logger = NullLogger<SignificantAveragingSellExecutor>.Instance;
            var executor = new SignificantAveragingSellExecutor(logger);
            var clearOpenOrdersExecutor = Mock.Of<IAlgoCommandExecutor<CancelOpenOrdersCommand>>();
            var provider = new ServiceCollection()
                .AddSingleton(clearOpenOrdersExecutor)
                .BuildServiceProvider();
            var context = new AlgoContext("Algo1", provider);
            var symbol = Symbol.Empty with
            {
                Name = "ABCXYZ",
                BaseAsset = "ABC",
                QuoteAsset = "XYZ",
                Filters = SymbolFilters.Empty with
                {
                    Price = PriceSymbolFilter.Empty with
                    {
                        TickSize = 1m
                    },
                    LotSize = LotSizeSymbolFilter.Empty with
                    {
                        StepSize = 1m,
                        MinQuantity = 100m
                    },
                    MinNotional = MinNotionalSymbolFilter.Empty with
                    {
                        MinNotional = decimal.MaxValue
                    }
                }
            };
            var ticker = MiniTicker.Empty with
            {
                Symbol = symbol.Name,
                ClosePrice = 50000
            };
            var orders = new[]
            {
                OrderQueryResult.Empty with
                {
                    Symbol = symbol.Name,
                    Side = OrderSide.Buy,
                    ExecutedQuantity = 100m,
                    Price = 50000m
                },
                OrderQueryResult.Empty with
                {
                    Symbol = symbol.Name,
                    Side = OrderSide.Buy,
                    ExecutedQuantity = 100m,
                    Price = 40000m
                }
            };
            var minimumProfitRate = 1.01m;
            var redeemSavings = false;
            var redeemSwapPool = true;
            var command = new SignificantAveragingSellCommand(symbol, ticker, orders, minimumProfitRate, redeemSavings, redeemSwapPool);

            // act
            await executor.ExecuteAsync(context, command, CancellationToken.None);

            // assert
            Mock.Get(clearOpenOrdersExecutor)
                .Verify(x => x.ExecuteAsync(context, It.Is<CancelOpenOrdersCommand>(x => x.Side == OrderSide.Sell && x.Symbol == symbol), CancellationToken.None));
        }

        [Fact]
        public async Task EnsuresSingleOrderOnSellableOrders()
        {
            // arrange
            var symbol = Symbol.Empty with
            {
                Name = "ABCXYZ",
                BaseAsset = "ABC",
                QuoteAsset = "XYZ",
                Filters = SymbolFilters.Empty with
                {
                    Price = PriceSymbolFilter.Empty with
                    {
                        TickSize = 1m
                    },
                    LotSize = LotSizeSymbolFilter.Empty with
                    {
                        StepSize = 1m,
                        MinQuantity = 100m
                    },
                    MinNotional = MinNotionalSymbolFilter.Empty with
                    {
                        MinNotional = 1m
                    }
                }
            };

            var logger = NullLogger<SignificantAveragingSellExecutor>.Instance;
            var executor = new SignificantAveragingSellExecutor(logger);
            var clearOpenOrdersExecutor = Mock.Of<IAlgoCommandExecutor<CancelOpenOrdersCommand>>();
            var ensureSingleOrderExecutor = Mock.Of<IAlgoCommandExecutor<EnsureSingleOrderCommand>>();
            var provider = new ServiceCollection()
                .AddSingleton(clearOpenOrdersExecutor)
                .AddSingleton(ensureSingleOrderExecutor)
                .BuildServiceProvider();
            var context = new AlgoContext("Algo1", provider)
            {
                Symbol = symbol,
                SpotBalancesLookup =
                {
                    [symbol.Name] = SymbolSpotBalances.Empty with
                    {
                        BaseAsset = Balance.Zero(symbol.BaseAsset) with { Free = 200m }
                    }
                }
            };

            var ticker = MiniTicker.Empty with
            {
                Symbol = symbol.Name,
                ClosePrice = 50000
            };
            var orders = new[]
            {
                OrderQueryResult.Empty with
                {
                    Symbol = symbol.Name,
                    Side = OrderSide.Buy,
                    ExecutedQuantity = 100m,
                    Price = 50000m
                },
                OrderQueryResult.Empty with
                {
                    Symbol = symbol.Name,
                    Side = OrderSide.Buy,
                    ExecutedQuantity = 100m,
                    Price = 40000m
                }
            };
            var minimumProfitRate = 1.01m;
            var redeemSavings = false;
            var redeemSwapPool = true;
            var command = new SignificantAveragingSellCommand(symbol, ticker, orders, minimumProfitRate, redeemSavings, redeemSwapPool);

            // act
            await executor.ExecuteAsync(context, command, CancellationToken.None);

            // assert
            Mock.Get(ensureSingleOrderExecutor)
                .Verify(x => x.ExecuteAsync(
                    context,
                    It.Is<EnsureSingleOrderCommand>(x =>
                        x.Symbol == symbol &&
                        x.Side == OrderSide.Sell &&
                        x.Type == OrderType.Limit &&
                        x.TimeInForce == TimeInForce.GoodTillCanceled &&
                        x.Quantity == 200m &&
                        x.Price == 50000m &&
                        !x.RedeemSavings),
                    CancellationToken.None));
        }
    }
}