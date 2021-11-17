using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Orleans;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.ProfitAggregator;
using Outcompute.Trader.Trading.Providers;
using System.Collections.Immutable;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class AlgoStatisticsPublisherTests
    {
        [Fact]
        public async Task Publishes()
        {
            // arrange
            var symbol = Symbol.Empty with { Name = "ABCXYZ", BaseAsset = "ABC", QuoteAsset = "XYZ" };

            var logger = NullLogger<AlgoStatisticsPublisher>.Instance;

            var factory = Mock.Of<IGrainFactory>();
            Mock.Get(factory)
                .Setup(x => x.GetGrain<IProfitAggregatorLocalGrain>(Guid.Empty, null).PublishAsync(It.IsAny<Profit>()))
                .Verifiable();

            var balances = Mock.Of<IBalanceProvider>();
            Mock.Get(balances)
                .Setup(x => x.TryGetBalanceAsync(symbol.BaseAsset, CancellationToken.None))
                .ReturnsAsync(Balance.Zero(symbol.BaseAsset))
                .Verifiable();

            var savings = Mock.Of<ISavingsProvider>();
            Mock.Get(savings)
                .Setup(x => x.TryGetPositionAsync(symbol.BaseAsset, CancellationToken.None))
                .ReturnsAsync(SavingsPosition.Zero(symbol.BaseAsset))
                .Verifiable();

            var swaps = Mock.Of<ISwapPoolProvider>();
            Mock.Get(swaps)
                .Setup(x => x.GetBalanceAsync(symbol.BaseAsset, CancellationToken.None))
                .ReturnsAsync(SwapPoolAssetBalance.Zero(symbol.BaseAsset))
                .Verifiable();

            var publisher = new AlgoStatisticsPublisher(logger, factory, balances, savings, swaps);
            var order = OrderQueryResult.Empty with { Symbol = symbol.Name, OrderId = 123, ExecutedQuantity = 10, Price = 100 };
            var significant = AutoPosition.Empty with
            {
                Symbol = symbol,
                Orders = ImmutableSortedSet.Create(order)
            };
            var ticker = MiniTicker.Empty with { Symbol = "ABCXYZ" };

            // act
            await publisher.PublishAsync(significant, ticker, CancellationToken.None);

            // assert
            Mock.Get(factory).VerifyAll();
            Mock.Get(balances).VerifyAll();
            Mock.Get(savings).VerifyAll();
            Mock.Get(swaps).VerifyAll();
        }
    }
}