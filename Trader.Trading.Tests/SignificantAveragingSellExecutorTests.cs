using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Commands;
using Outcompute.Trader.Trading.Commands.CancelOpenOrders;
using Outcompute.Trader.Trading.Commands.EnsureSingleOrder;
using Outcompute.Trader.Trading.Commands.SignificantAveragingSell;
using System.Collections.Immutable;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class SignificantAveragingSellExecutorTests
    {
        [Fact]
        public async Task ClearsSellOrdersOnNoBuyOrders()
        {
            // arrange
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

            var logger = NullLogger<SignificantAveragingSellExecutor>.Instance;
            var executor = new SignificantAveragingSellExecutor(logger);
            var clearOpenOrdersExecutor = Mock.Of<IAlgoCommandExecutor<CancelOpenOrdersCommand>>();
            var provider = new ServiceCollection()
                .AddSingleton(clearOpenOrdersExecutor)
                .BuildServiceProvider();
            var context = new AlgoContext("Algo1", provider)
            {
                Symbols = { [symbol.Name] = symbol },
                Tickers = { [symbol.Name] = ticker },
                SpotBalances = { [symbol.Name] = SymbolSpotBalances.Empty },
                SavingsBalances = { [symbol.Name] = SymbolSavingsBalances.Empty },
                AutoPositions =
                {
                    [symbol.Name] = AutoPosition.Empty with
                    {
                        Orders = orders
                    }
                }
            };

            var minimumProfitRate = 1.01m;
            var redeemSavings = false;
            var redeemSwapPool = true;
            var command = new SignificantAveragingSellCommand(symbol, minimumProfitRate, redeemSavings, redeemSwapPool);

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
            var context = new AlgoContext("Algo1", provider)
            {
                Symbols = { [symbol.Name] = symbol },
                SpotBalances = { [symbol.Name] = SymbolSpotBalances.Empty },
                SavingsBalances = { [symbol.Name] = SymbolSavingsBalances.Empty },
                Tickers = { [symbol.Name] = ticker },
                AutoPositions =
                {
                    [symbol.Name] = AutoPosition.Empty with
                    {
                        Orders = orders.ToImmutableSortedSet()
                    }
                }
            };
            var minimumProfitRate = 1.01m;
            var redeemSavings = false;
            var redeemSwapPool = true;
            var command = new SignificantAveragingSellCommand(symbol, minimumProfitRate, redeemSavings, redeemSwapPool);

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

            var context = new AlgoContext("Algo1", provider)
            {
                Symbols = { [symbol.Name] = symbol },
                Tickers = { [symbol.Name] = ticker },
                SpotBalances = { [symbol.Name] = SymbolSpotBalances.Empty },
                SavingsBalances = { [symbol.Name] = SymbolSavingsBalances.Empty },
                AutoPositions =
                {
                    [symbol.Name] = AutoPosition.Empty with
                    {
                        Orders = orders.ToImmutableSortedSet(OrderQueryResult.KeyComparer)
                    }
                }
            };

            var minimumProfitRate = 1.01m;
            var redeemSavings = false;
            var redeemSwapPool = true;
            var command = new SignificantAveragingSellCommand(symbol, minimumProfitRate, redeemSavings, redeemSwapPool);

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

            var context = new AlgoContext("Algo1", provider)
            {
                Symbols = { [symbol.Name] = symbol },
                Tickers = { [symbol.Name] = ticker },
                SpotBalances = { [symbol.Name] = SymbolSpotBalances.Empty },
                SavingsBalances = { [symbol.Name] = SymbolSavingsBalances.Empty },
                AutoPositions =
                {
                    [symbol.Name] = AutoPosition.Empty with
                    {
                        Orders = orders.ToImmutableSortedSet(OrderQueryResult.KeyComparer)
                    }
                }
            };

            var minimumProfitRate = 1.01m;
            var redeemSavings = false;
            var redeemSwapPool = true;
            var command = new SignificantAveragingSellCommand(symbol, minimumProfitRate, redeemSavings, redeemSwapPool);

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

            var ticker = MiniTicker.Empty with
            {
                Symbol = symbol.Name,
                ClosePrice = 50000
            };
            var orders = ImmutableSortedSet.Create(OrderQueryResult.KeyComparer, new[]
            {
                OrderQueryResult.Empty with
                {
                    Symbol = symbol.Name,
                    OrderId = 1,
                    Side = OrderSide.Buy,
                    ExecutedQuantity = 100m,
                    Price = 50000m
                },
                OrderQueryResult.Empty with
                {
                    Symbol = symbol.Name,
                    OrderId = 2,
                    Side = OrderSide.Buy,
                    ExecutedQuantity = 100m,
                    Price = 40000m
                }
            });

            var context = new AlgoContext("Algo1", provider)
            {
                Symbols = { [symbol.Name] = symbol },
                Tickers = { [symbol.Name] = ticker },
                AutoPositions =
                {
                    [symbol.Name] = AutoPosition.Empty with
                    {
                        Orders = orders
                    }
                },
                SpotBalances =
                {
                    [symbol.Name] = SymbolSpotBalances.Empty with
                    {
                        BaseAsset = Balance.Zero(symbol.BaseAsset) with { Free = 200m }
                    }
                },
                SavingsBalances = { [symbol.Name] = SymbolSavingsBalances.Empty }
            };

            var minimumProfitRate = 1.01m;
            var redeemSavings = false;
            var redeemSwapPool = true;
            var command = new SignificantAveragingSellCommand(symbol, minimumProfitRate, redeemSavings, redeemSwapPool);

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