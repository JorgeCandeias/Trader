using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Orleans;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.ProfitAggregator;
using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class AlgoStatisticsPublisherTests
    {
        [Fact]
        public async Task Publishes()
        {
            // arrange
            var logger = NullLogger<AlgoStatisticsPublisher>.Instance;

            var factory = Mock.Of<IGrainFactory>();
            Mock.Get(factory)
                .Setup(x => x.GetGrain<IProfitAggregatorLocalGrain>(Guid.Empty, null).PublishAsync(It.IsAny<Profit>()))
                .Verifiable();

            var publisher = new AlgoStatisticsPublisher(logger, factory);
            var symbol = Symbol.Empty with { Name = "ABCXYZ" };
            var order = OrderQueryResult.Empty with { Symbol = symbol.Name, OrderId = 123 };
            var significant = SignificantResult.Empty with
            {
                Symbol = symbol,
                Orders = ImmutableSortedSet.Create(order)
            };
            var ticker = MiniTicker.Empty with { Symbol = "ABCXYZ" };

            // act
            await publisher.PublishAsync(significant, ticker, CancellationToken.None);

            // assert
            Mock.Get(factory).VerifyAll();
        }
    }
}