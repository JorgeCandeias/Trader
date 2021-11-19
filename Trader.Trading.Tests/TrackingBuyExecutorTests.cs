using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Commands;
using Outcompute.Trader.Trading.Commands.CancelOrder;
using Outcompute.Trader.Trading.Commands.RedeemSavings;
using Outcompute.Trader.Trading.Commands.TrackingBuy;
using Outcompute.Trader.Trading.Providers;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class TrackingBuyExecutorTests
    {
        [Fact]
        public async Task ExecutesWithSavingsRedemption()
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
                        StepSize = 0.000001m
                    },
                    Price = PriceSymbolFilter.Empty with
                    {
                        TickSize = 1
                    }
                }
            };

            var executor = new TrackingBuyExecutor(NullLogger<TrackingBuyExecutor>.Instance);

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
            context.Data.GetOrAdd(symbol.Name).Ticker = MiniTicker.Empty with { Symbol = symbol.Name, ClosePrice = 12345.678m };
            context.Data.GetOrAdd(symbol.Name).Spot.QuoteAsset = Balance.Empty with { Asset = symbol.QuoteAsset, Free = 10m };
            context.Data.GetOrAdd(symbol.Name).Savings.QuoteAsset = SavingsBalance.Empty with { Asset = symbol.QuoteAsset, FreeAmount = 2990m };
            context.Data.GetOrAdd(symbol.Name).Orders.Open = new OrderCollection(new[]
            {
                OrderQueryResult.Empty with { Symbol = symbol.Name, OrderId = 1, Side = OrderSide.Buy, Status = OrderStatus.New, Price = 12000m }
            });
            context.Data.GetOrAdd(symbol.Name).SwapPools.QuoteAsset = SwapPoolAssetBalance.Empty with { Total = 0 };

            var pullbackRatio = 0.999m;
            var targetQuoteBalanceFractionPerBuy = 0.01m;
            var maxNotional = 100m;
            var redeemSavings = true;
            var redeemSwapPool = true;
            var command = new TrackingBuyCommand(symbol, pullbackRatio, targetQuoteBalanceFractionPerBuy, maxNotional, redeemSavings, redeemSwapPool);

            // act
            await executor.ExecuteAsync(context, command);

            // assert
            Mock.Get(cancelOrderExecutor).Verify(x => x.ExecuteAsync(context, It.Is<CancelOrderCommand>(x => x.Symbol == symbol && x.OrderId == 1), CancellationToken.None));
            Mock.Get(redeemSavingsExecutor).Verify(x => x.ExecuteAsync(context, It.Is<RedeemSavingsCommand>(x => x.Asset == symbol.QuoteAsset && x.Amount == 20.006189M), CancellationToken.None));
        }
    }
}