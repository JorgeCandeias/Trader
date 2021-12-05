using Orleans;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers.Tickers;

namespace Outcompute.Trader.Trading.Tests
{
    public class TickerProviderTests
    {
        [Fact]
        public async Task SetsTicker()
        {
            // arrange
            var symbol = "ABCXYZ";
            var ticker = MiniTicker.Empty with { Symbol = symbol };

            var factory = Mock.Of<IGrainFactory>();
            Mock.Get(factory)
                .Setup(x => x.GetGrain<ITickerProviderReplicaGrain>(symbol, null).SetTickerAsync(ticker))
                .Verifiable();

            var provider = new TickerProvider(factory);

            // act
            await provider.SetTickerAsync(ticker);

            // assert
            Mock.Get(factory).VerifyAll();
        }

        [Fact]
        public async Task GetsTicker()
        {
            // arrange
            var symbol = "ABCXYZ";
            var ticker = MiniTicker.Empty with { Symbol = symbol };

            var factory = Mock.Of<IGrainFactory>();
            Mock.Get(factory)
                .Setup(x => x.GetGrain<ITickerProviderReplicaGrain>(symbol, null).TryGetTickerAsync())
                .ReturnsAsync(ticker)
                .Verifiable();

            var provider = new TickerProvider(factory);

            // act
            var result = await provider.TryGetTickerAsync(symbol);

            // assert
            Assert.Same(ticker, result);
            Mock.Get(factory).VerifyAll();
        }
    }
}