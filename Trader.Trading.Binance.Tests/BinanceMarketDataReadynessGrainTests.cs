using Moq;
using Orleans;
using Orleans.TestingHost;
using Outcompute.Trader.Trading.Binance.Providers.MarketData;
using Outcompute.Trader.Trading.Binance.Tests.Fakes;
using Outcompute.Trader.Trading.Binance.Tests.Fixtures;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.Binance.Tests
{
    [Collection(nameof(ClusterCollectionFixture))]
    public class BinanceMarketDataReadynessGrainTests
    {
        private readonly TestCluster _cluster;

        public BinanceMarketDataReadynessGrainTests(ClusterFixture cluster)
        {
            _cluster = cluster?.Cluster ?? throw new ArgumentNullException(nameof(cluster));
        }

        [Fact]
        public async Task IsReadySignalsTrue()
        {
            // arrange
            var state = false;
            var factory = Mock.Of<IGrainFactory>();
            Mock.Get(factory)
                .Setup(x => x.GetGrain<IBinanceMarketDataGrain>(Guid.Empty, null).IsReadyAsync())
                .Returns(() => Task.FromResult(state))
                .Verifiable();

            var timers = new FakeTimerRegistry();

            var grain = new BinanceMarketDataReadynessGrain(factory, timers);

            // act
            await grain.OnActivateAsync();

            // assert timer was created
            var timer = Assert.Single(timers.Entries);
            Assert.Same(grain, timer.Grain);
            Assert.Equal(TimeSpan.FromSeconds(1), timer.DueTime);
            Assert.Equal(TimeSpan.FromSeconds(1), timer.Period);

            // act - get ready state
            var ready = await grain.IsReadyAsync();

            // assert current ready state is false
            Assert.Equal(state, ready);

            // arrange dependant state
            state = true;

            // act - simulate tick
            await timer.AsyncCallback(timer.State);

            // act - get ready state
            ready = await grain.IsReadyAsync();

            // assert current ready state is true
            Assert.Equal(state, ready);

            // act - simulate deactivation
            await grain.OnDeactivateAsync();

            // assert everything was called
            Mock.Get(factory).VerifyAll();
        }
    }
}