using Moq;
using Orleans;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Binance.Providers.MarketData;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.Binance.Tests
{
    public class BinanceTickerProviderTests
    {
        [Fact]
        public async Task TryGetTickerAsync()
        {
            // arrange
            var symbol = "ABCXYZ";
            var ticker = MiniTicker.Empty with { Symbol = symbol, ClosePrice = 123m };
            var grain = Mock.Of<IBinanceTickerProviderGrain>(x => x.TryGetTickerAsync() == ValueTask.FromResult<MiniTicker?>(ticker));
            var factory = Mock.Of<IGrainFactory>(x => x.GetGrain<IBinanceTickerProviderGrain>(symbol, null) == grain);
            var provider = new BinanceTickerProvider(factory);

            // act
            var result = await provider.TryGetTickerAsync(symbol, CancellationToken.None);

            // assert
            Assert.Equal(ticker, result);
        }
    }
}