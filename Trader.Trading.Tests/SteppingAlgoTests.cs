using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Algorithms.Stepping;
using Outcompute.Trader.Trading.Commands;
using Outcompute.Trader.Trading.Commands.ClearOpenOrders;
using Outcompute.Trader.Trading.Commands.CreateOrder;
using Outcompute.Trader.Trading.Commands.RedeemSavings;
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

            var options = Options.Create(new SteppingAlgoOptions
            {
                IsOpeningEnabled = false
            });

            var symbol = Symbol.Empty with
            {
                Name = "ABCXYZ",
                BaseAsset = "ABC",
                QuoteAsset = "XYZ"
            };

            var algo = new SteppingAlgo(logger, options);

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
            var command = Assert.IsType<ClearOpenOrdersCommand>(result);
            Assert.Equal(symbol, command.Symbol);
            Assert.Equal(OrderSide.Buy, command.Side);
        }

        [Fact]
        public async Task ReturnsNoopOnRedeemedSavings()
        {
            // arrange
            var name = "MyAlgo";
            var logger = NullLogger<SteppingAlgo>.Instance;

            var options = Options.Create(new SteppingAlgoOptions
            {
                IsOpeningEnabled = true
            });

            var symbol = Symbol.Empty with
            {
                Name = "ABCXYZ",
                BaseAsset = "ABC",
                QuoteAsset = "XYZ",
                Filters = SymbolFilters.Empty with
                {
                    Price = PriceSymbolFilter.Empty with
                    {
                        TickSize = 0.0000001m
                    },
                    MinNotional = MinNotionalSymbolFilter.Empty with
                    {
                        MinNotional = 0.00010000m
                    }
                }
            };

            var algo = new SteppingAlgo(logger, options);

            var redeemSavingsExecutor = Mock.Of<IAlgoCommandExecutor<RedeemSavingsCommand, RedeemSavingsEvent>>();
            Mock.Get(redeemSavingsExecutor)
                .Setup(x => x.ExecuteAsync(It.IsAny<IAlgoContext>(), It.Is<RedeemSavingsCommand>(x => x.Asset == symbol.QuoteAsset && x.Amount == 0.00010000m), CancellationToken.None))
                .Returns(Task.FromResult(new RedeemSavingsEvent(true, 0m)))
                .Verifiable();

            var provider = new ServiceCollection()
                .AddSingleton(redeemSavingsExecutor)
                .BuildServiceProvider();

            algo.Context = new AlgoContext(provider)
            {
                Name = name,
                Symbol = symbol,
                Ticker = MiniTicker.Empty with
                {
                    Symbol = symbol.Name,
                    ClosePrice = 10000m
                }
            };

            // act
            var result = await algo.GoAsync();

            // arrange
            Mock.Get(redeemSavingsExecutor).VerifyAll();

            Assert.IsType<NoopAlgoCommand>(result);
        }

        [Fact]
        public async Task CreatesStartingBuyOrder()
        {
            // arrange
            var name = "MyAlgo";
            var logger = NullLogger<SteppingAlgo>.Instance;

            var options = Options.Create(new SteppingAlgoOptions
            {
                IsOpeningEnabled = true
            });

            var symbol = Symbol.Empty with
            {
                Name = "ABCXYZ",
                BaseAsset = "ABC",
                QuoteAsset = "XYZ",
                Filters = SymbolFilters.Empty with
                {
                    Price = PriceSymbolFilter.Empty with
                    {
                        TickSize = 0.0000001m
                    },
                    MinNotional = MinNotionalSymbolFilter.Empty with
                    {
                        MinNotional = 0.00010000m
                    },
                    LotSize = LotSizeSymbolFilter.Empty with
                    {
                        StepSize = 0.00000010m
                    }
                }
            };

            var algo = new SteppingAlgo(logger, options);

            var redeemSavingsExecutor = Mock.Of<IAlgoCommandExecutor<RedeemSavingsCommand, RedeemSavingsEvent>>();
            Mock.Get(redeemSavingsExecutor)
                .Setup(x => x.ExecuteAsync(It.IsAny<IAlgoContext>(), It.Is<RedeemSavingsCommand>(x => x.Asset == symbol.QuoteAsset && x.Amount == 0.00010000m), CancellationToken.None))
                .Returns(Task.FromResult(new RedeemSavingsEvent(true, 0m)))
                .Verifiable();

            var provider = new ServiceCollection()
                .AddSingleton(redeemSavingsExecutor)
                .BuildServiceProvider();

            algo.Context = new AlgoContext(provider)
            {
                Name = name,
                Symbol = symbol,
                Ticker = MiniTicker.Empty with
                {
                    Symbol = symbol.Name,
                    ClosePrice = 10000m
                },
                QuoteSpotBalance = Balance.Empty with
                {
                    Free = 0.00010000m
                },
            };

            // act
            var result = await algo.GoAsync();

            // arrange
            var command = Assert.IsType<CreateOrderCommand>(result);
            Assert.Equal(symbol, command.Symbol);
            Assert.Equal(OrderType.Limit, command.Type);
            Assert.Equal(OrderSide.Buy, command.Side);
            Assert.Equal(TimeInForce.GoodTillCanceled, command.TimeInForce);
            Assert.Equal(0.00000010m, command.Quantity);
            Assert.Equal(10000m, command.Price);
            Assert.Equal("ABCXYZ1000000000000", command.Tag);
        }
    }
}