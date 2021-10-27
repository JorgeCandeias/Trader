using Orleans.TestingHost;
using Outcompute.Trader.Trading.Binance.Tests.Fixtures;
using System;
using Xunit;

namespace Outcompute.Trader.Trading.Binance.Tests
{
    [Collection(nameof(ClusterCollectionFixture))]
    public class BinanceMarketDataGrainTests
    {
        private readonly TestCluster _cluster;

        public BinanceMarketDataGrainTests(ClusterFixture cluster)
        {
            _cluster = cluster?.Cluster ?? throw new ArgumentNullException(nameof(cluster));
        }

        /*
        [Fact]
        public async Task StreamingWorks()
        {
            // arrange
            var options = Options.Create(new BinanceOptions());
            var logger = NullLogger<BinanceMarketDataGrain>.Instance;
            var factory = Mock.Of<IMarketDataStreamClientFactory>();

            var ticker = Ticker.Empty with { Symbol = "ABCXYZ", CloseTime = DateTime.UtcNow, LastPrice = 123.456m };

            var trader = Mock.Of<ITradingService>();
            Mock.Get(trader)
                .Setup(x => x.WithBackoff())
                .Returns(trader)
                .Verifiable();
            Mock.Get(trader)
                .Setup(x => x.Get24hTickerPriceChangeStatisticsAsync("ABCXYZ", CancellationToken.None))
                .Returns(Task.FromResult(ticker))
                .Verifiable();

            var mapper = Mock.Of<IMapper>();
            var clock = Mock.Of<ISystemClock>();

            var dependencies = Mock.Of<IAlgoDependencyInfo>();
            Mock.Get(dependencies)
                .Setup(x => x.GetTickers())
                .Returns(new[] { "ABCXYZ" })
                .Verifiable();
            Mock.Get(dependencies)
                .Setup(x => x.GetKlines())
                .Returns(new[]
                {
                    new KlineDependency("ABCXYZ", KlineInterval.Days1, 100),
                    new KlineDependency("AAAZZZ", KlineInterval.Hours1, 200)
                })
                .Verifiable();

            var lifetime = Mock.Of<IHostApplicationLifetime>();
            var klines = Mock.Of<IKlineProvider>();
            var tickers = Mock.Of<ITickerProvider>();

            var timers = new FakeTimerRegistry();

            var grain = new Mock<BinanceMarketDataGrain>(() => new BinanceMarketDataGrain(options, logger, factory, trader, mapper, clock, dependencies, lifetime, klines, tickers, timers))
            {
                CallBase = true
            };

            // act - activate
            await grain.Object.OnActivateAsync();

            // assert timer was registered
            var timer = Assert.Single(timers.Entries);
            Assert.Same(grain.Object, timer.Grain);
            Assert.NotNull(timer.AsyncCallback);
            Assert.Null(timer.State);
            Assert.Equal(TimeSpan.FromSeconds(1), timer.DueTime);
            Assert.Equal(TimeSpan.FromSeconds(1), timer.Period);

            // act - simulate tick
            await timer.AsyncCallback(timer.State);

            // allow the stream to work
            await Task.Delay(100);

            // assert the synced ticker was received
            Mock.Get(tickers)
                .Verify(x => x.SetTickerAsync(It.Is<MiniTicker>(x => x.Symbol == ticker.Symbol && x.ClosePrice == ticker.LastPrice), CancellationToken.None));

            // assert
            Mock.Get(dependencies).VerifyAll();
            Mock.Get(trader).VerifyAll();
        }
        */
    }
}