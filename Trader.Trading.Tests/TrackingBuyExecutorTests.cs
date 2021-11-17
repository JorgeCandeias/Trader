using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Commands;
using Outcompute.Trader.Trading.Commands.CancelOrder;
using Outcompute.Trader.Trading.Commands.RedeemSavings;
using Outcompute.Trader.Trading.Commands.TrackingBuy;
using Outcompute.Trader.Trading.Providers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class TrackingBuyExecutorTests
    {
        [Fact]
        public async Task ExecutesWithSavingsRedemption()
        {
            // arrange
            var logger = NullLogger<TrackingBuyExecutor>.Instance;

            var ticker = MiniTicker.Empty with { Symbol = "ABCXYZ", ClosePrice = 12345.678m };
            var tickers = Mock.Of<ITickerProvider>();
            Mock.Get(tickers)
                .Setup(x => x.TryGetTickerAsync("ABCXYZ", CancellationToken.None))
                .Returns(Task.FromResult<MiniTicker?>(ticker))
                .Verifiable();

            var balance = Balance.Empty with { Asset = "XYZ", Free = 10m };
            var balances = Mock.Of<IBalanceProvider>();
            Mock.Get(balances)
                .Setup(x => x.TryGetBalanceAsync("XYZ", CancellationToken.None))
                .ReturnsAsync(balance)
                .Verifiable();

            var position = SavingsBalance.Empty with { Asset = "XYZ", FreeAmount = 2990m };
            var savings = Mock.Of<ISavingsProvider>();
            Mock.Get(savings)
                .Setup(x => x.TryGetBalanceAsync("XYZ", CancellationToken.None))
                .ReturnsAsync(position)
                .Verifiable();

            var active1 = OrderQueryResult.Empty with { Symbol = "ABCXYZ", OrderId = 1, Side = OrderSide.Buy, Status = OrderStatus.New, Price = 12000m };
            var orders = Mock.Of<IOrderProvider>();
            Mock.Get(orders)
                .Setup(x => x.GetOrdersByFilterAsync("ABCXYZ", OrderSide.Buy, true, null, CancellationToken.None))
                .Returns(Task.FromResult<IReadOnlyList<OrderQueryResult>>(new[] { active1 }))
                .Verifiable();

            var swaps = Mock.Of<ISwapPoolProvider>();
            Mock.Get(swaps)
                .Setup(x => x.GetBalanceAsync("XYZ", CancellationToken.None))
                .ReturnsAsync(SwapPoolAssetBalance.Empty)
                .Verifiable();

            var executor = new TrackingBuyExecutor(logger, tickers, orders, balances, savings, swaps);

            var cancelOrderExecutor = Mock.Of<IAlgoCommandExecutor<CancelOrderCommand>>();

            var redeemSavingsExecutor = Mock.Of<IAlgoCommandExecutor<RedeemSavingsCommand, RedeemSavingsEvent>>();

            var redeemed = new RedeemSavingsEvent(true, 20m);
            Mock.Get(redeemSavingsExecutor)
                .Setup(x => x.ExecuteAsync(It.IsAny<IAlgoContext>(), It.IsAny<RedeemSavingsCommand>(), CancellationToken.None))
                .ReturnsAsync(redeemed);

            var provider = new ServiceCollection()
                .AddSingleton(cancelOrderExecutor)
                .AddSingleton(redeemSavingsExecutor)
                .BuildServiceProvider();

            var context = new AlgoContext("Algo1", provider);

            var symbol = Symbol.Empty with
            {
                Name = "ABCXYZ",
                BaseAsset = "ABC",
                QuoteAsset = "XYZ",
                Filters = SymbolFilters.Empty with
                {
                    LotSize = LotSizeSymbolFilter.Empty with
                    {
                        StepSize = 0.000001m
                    },
                    Price = PriceSymbolFilter.Empty with
                    {
                        TickSize = 1
                    }
                }
            };

            var pullbackRatio = 0.999m;
            var targetQuoteBalanceFractionPerBuy = 0.01m;
            var maxNotional = 100m;
            var redeemSavings = true;
            var redeemSwapPool = true;
            var command = new TrackingBuyCommand(symbol, pullbackRatio, targetQuoteBalanceFractionPerBuy, maxNotional, redeemSavings, redeemSwapPool);

            // act
            await executor.ExecuteAsync(context, command);

            // assert
            Mock.Get(tickers).VerifyAll();
            Mock.Get(orders).VerifyAll();
            Mock.Get(balances).VerifyAll();
            Mock.Get(savings).VerifyAll();
            Mock.Get(swaps).VerifyAll();
            Mock.Get(cancelOrderExecutor).Verify(x => x.ExecuteAsync(context, It.Is<CancelOrderCommand>(x => x.Symbol == symbol && x.OrderId == 1), CancellationToken.None));
            Mock.Get(redeemSavingsExecutor).Verify(x => x.ExecuteAsync(context, It.Is<RedeemSavingsCommand>(x => x.Asset == "XYZ" && x.Amount == 20.006189M), CancellationToken.None));
        }
    }
}