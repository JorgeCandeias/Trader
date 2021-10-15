using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Providers;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class AveragingSellBlockTests
    {
        [Fact]
        public async Task ThrowsOnNullContext()
        {
            // arrange
            IAlgoContext context = null!;

            // act
            Task TestCode() => context.SetAveragingSellAsync(null!, null!, 0m, false, CancellationToken.None).AsTask();

            // assert
            await Assert.ThrowsAsync<ArgumentNullException>("context", TestCode);
        }

        [Fact]
        public async Task ThrowsOnNullSymbol()
        {
            // arrange
            var context = Mock.Of<IAlgoContext>();

            // act
            Task TestCode() => context.SetAveragingSellAsync(null!, null!, 0m, false, CancellationToken.None).AsTask();

            // assert
            await Assert.ThrowsAsync<ArgumentNullException>("symbol", TestCode);
        }

        [Fact]
        public async Task ThrowsOnNullOrders()
        {
            // arrange
            var context = Mock.Of<IAlgoContext>();
            var symbol = Symbol.Empty;

            // act
            Task TestCode() => context.SetAveragingSellAsync(symbol, null!, 0m, false, CancellationToken.None).AsTask();

            // assert
            await Assert.ThrowsAsync<ArgumentNullException>("orders", TestCode);
        }

        [Fact]
        public async Task ThrowsOnNonBuyOrder()
        {
            // arrange
            var context = Mock.Of<IAlgoContext>();
            var symbol = Symbol.Empty;
            var orders = new[] { OrderQueryResult.Empty with { OrderId = 123, Side = OrderSide.Sell } };

            // act
            Task TestCode() => context.SetAveragingSellAsync(symbol, orders, 0m, false, CancellationToken.None).AsTask();

            // assert
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>("orders", TestCode);
        }

        [Fact]
        public async Task ThrowsOnNonSignificantOrder()
        {
            // arrange
            var context = Mock.Of<IAlgoContext>();
            var symbol = Symbol.Empty;
            var orders = new[] { OrderQueryResult.Empty with { OrderId = 123, Side = OrderSide.Buy, ExecutedQuantity = 0m } };

            // act
            Task TestCode() => context.SetAveragingSellAsync(symbol, orders, 0m, false, CancellationToken.None).AsTask();

            // assert
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>("orders", TestCode);
        }

        [Fact]
        public async Task HandlesNoTicker()
        {
            // arrange
            var transient = ImmutableSortedOrderSet.Create(new[] { OrderQueryResult.Empty with { OrderId = 999, Side = OrderSide.Sell, Symbol = "ABCXYZ" } });

            var repository = Mock.Of<ITradingRepository>(x =>
                x.GetTransientOrdersBySideAsync("ABCXYZ", OrderSide.Sell, CancellationToken.None) == Task.FromResult(transient));

            var trader = Mock.Of<ITradingService>();

            var provider = new ServiceCollection()
                .AddSingleton(repository)
                .AddSingleton(Mock.Of<ISavingsProvider>())
                .AddSingleton(Mock.Of<ITickerProvider>())
                .AddSingleton(Mock.Of<ILogger<IAlgoContext>>())
                .AddSingleton(trader)
                .BuildServiceProvider();

            var context = Mock.Of<IAlgoContext>(x => x.ServiceProvider == provider);

            var symbol = Symbol.Empty with { Name = "ABCXYZ", BaseAsset = "ABC", QuoteAsset = "XYZ" };

            var orders = new[] {
                OrderQueryResult.Empty with { OrderId = 123, Side = OrderSide.Buy, ExecutedQuantity = 1000m },
                OrderQueryResult.Empty with { OrderId = 234, Side = OrderSide.Buy, ExecutedQuantity = 2000m },
                OrderQueryResult.Empty with { OrderId = 345, Side = OrderSide.Buy, ExecutedQuantity = 3000m }
            };

            // act
            await context.SetAveragingSellAsync(symbol, orders, 0m, false, CancellationToken.None);

            // assert
            Mock.Get(trader).Verify(x => x.CancelOrderAsync("ABCXYZ", 999, CancellationToken.None));
        }

        [Fact]
        [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "Mock")]
        public async Task PlacesAveragingSell()
        {
            // arrange
            var symbol = Symbol.Empty with
            {
                Name = "ABCXYZ",
                BaseAsset = "ABC",
                QuoteAsset = "XYZ",
                Filters = SymbolFilters.Empty with
                {
                    LotSize = LotSizeSymbolFilter.Empty with
                    {
                        StepSize = 1m
                    },
                    Price = PriceSymbolFilter.Empty with
                    {
                        TickSize = 1m
                    },
                    PercentPrice = PercentPriceSymbolFilter.Empty with
                    {
                        MultiplierUp = 2m
                    }
                }
            };
            var balance = Balance.Zero(symbol.BaseAsset) with { Free = 2000 };
            var existing = OrderQueryResult.Empty with { Symbol = symbol.Name, OrderId = 999, Side = OrderSide.Sell };
            var position = FlexibleProductPosition.Zero(symbol.BaseAsset) with { FreeAmount = 4000, ProductId = "P1" };
            var quota = LeftDailyRedemptionQuotaOnFlexibleProduct.Empty with { Asset = symbol.BaseAsset, LeftQuota = 1000000m, MinRedemptionAmount = 1m };
            var cancellation = CancelStandardOrderResult.Empty with
            {
                Symbol = existing.Symbol,
                OrderId = existing.OrderId,
                Side = existing.Side
            };
            var created = OrderResult.Empty with
            {
                Symbol = symbol.Name,
                OrderId = 88888,
                Side = OrderSide.Sell,
                Type = OrderType.Limit,
                TimeInForce = TimeInForce.GoodTillCanceled,
                OriginalQuantity = 6000,
                Price = 876,
                ClientOrderId = "ABCXYZ87600000000"
            };
            var ticker = MiniTicker.Empty with { Symbol = symbol.Name, ClosePrice = 1000 };

            var repository = Mock.Of<ITradingRepository>();
            Mock.Get(repository)
                .Setup(x => x.TryGetBalanceAsync(symbol.BaseAsset, CancellationToken.None))
                .Returns(Task.FromResult<Balance?>(balance))
                .Verifiable();
            Mock.Get(repository)
                .Setup(x => x.GetTransientOrdersBySideAsync(symbol.Name, OrderSide.Sell, CancellationToken.None))
                .Returns(Task.FromResult(ImmutableSortedOrderSet.Create(new[] { existing })))
                .Verifiable();
            Mock.Get(repository)
                .Setup(x => x.SetOrderAsync(cancellation, CancellationToken.None))
                .Verifiable();
            Mock.Get(repository)
                .Setup(x => x.SetOrderAsync(created, 0m, 0m, 0m, CancellationToken.None))
                .Verifiable();

            var savings = Mock.Of<ISavingsProvider>();
            Mock.Get(savings)
                .Setup(x => x.TryGetFirstFlexibleProductPositionAsync(symbol.BaseAsset, CancellationToken.None))
                .Returns(ValueTask.FromResult<FlexibleProductPosition?>(position))
                .Verifiable();
            Mock.Get(savings)
                .Setup(x => x.TryGetLeftDailyRedemptionQuotaOnFlexibleProductAsync(symbol.BaseAsset, position.ProductId, FlexibleProductRedemptionType.Fast, CancellationToken.None))
                .Returns(ValueTask.FromResult<LeftDailyRedemptionQuotaOnFlexibleProduct?>(quota))
                .Verifiable();

            var trader = Mock.Of<ITradingService>();
            Mock.Get(trader)
                .Setup(x => x.CancelOrderAsync(existing.Symbol, existing.OrderId, CancellationToken.None))
                .Returns(Task.FromResult(cancellation))
                .Verifiable();
            Mock.Get(trader)
                .Setup(x => x.CreateOrderAsync(symbol.Name, OrderSide.Sell, OrderType.Limit, TimeInForce.GoodTillCanceled, 6000, null, 876, "ABCXYZ87600000000", null, null, CancellationToken.None))
                .Returns(Task.FromResult(created))
                .Verifiable();

            var tickers = Mock.Of<ITickerProvider>();
            Mock.Get(tickers)
                .Setup(x => x.TryGetTickerAsync(symbol.Name, CancellationToken.None))
                .Returns(ValueTask.FromResult<MiniTicker?>(ticker))
                .Verifiable();

            var options = Options.Create(new SavingsOptions { SavingsRedemptionDelay = TimeSpan.Zero });

            var provider = new ServiceCollection()
                .AddSingleton(repository)
                .AddSingleton(savings)
                .AddSingleton(tickers)
                .AddSingleton(Mock.Of<ILogger<IAlgoContext>>())
                .AddSingleton(trader)
                .AddSingleton(options)
                .AddSingleton(Mock.Of<ISystemClock>())
                .BuildServiceProvider();

            var context = Mock.Of<IAlgoContext>(x => x.ServiceProvider == provider);

            var orders = new[] {
                OrderQueryResult.Empty with { Symbol = symbol.Name, OrderId = 123, Side = OrderSide.Buy, ExecutedQuantity = 1000m, Price = 1000m },
                OrderQueryResult.Empty with { Symbol = symbol.Name, OrderId = 234, Side = OrderSide.Buy, ExecutedQuantity = 2000m, Price = 900m },
                OrderQueryResult.Empty with { Symbol = symbol.Name, OrderId = 345, Side = OrderSide.Buy, ExecutedQuantity = 3000m, Price = 800m }
            };

            // act
            await context.SetAveragingSellAsync(symbol, orders, 1.01m, true, CancellationToken.None);

            // assert
            Mock.Get(repository).VerifyAll();
            Mock.Get(savings).VerifyAll();
            Mock.Get(trader).VerifyAll();
            Mock.Get(tickers).VerifyAll();
        }
    }
}