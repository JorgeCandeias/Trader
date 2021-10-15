using Moq;
using Orleans;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Binance.Providers.MarketData;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Trader.Trading.Binance.Tests
{
    public class BinanceKlineProviderTests
    {
        [Fact]
        public async Task GetsKlines()
        {
            // arrange
            var symbol = "ABCXYZ";
            var interval = KlineInterval.Hours1;
            var start = DateTime.Today.AddHours(-1);
            var end = DateTime.Today;
            var kline = Kline.Empty with { Symbol = symbol, Interval = interval, OpenTime = start };
            var grain = Mock.Of<IBinanceKlineProviderGrain>(x => x.GetKlinesAsync(start, end) == ValueTask.FromResult<IReadOnlyCollection<Kline>>(new[] { kline }));
            var factory = Mock.Of<IGrainFactory>(x => x.GetGrain<IBinanceKlineProviderGrain>($"{symbol}|{interval}", null) == grain);
            var provider = new BinanceKlineProvider(factory);

            // act
            var result = await provider.GetKlinesAsync(symbol, interval, start, end, CancellationToken.None);

            // assert
            Assert.Collection(result, x => Assert.Same(kline, x));
        }
    }
}