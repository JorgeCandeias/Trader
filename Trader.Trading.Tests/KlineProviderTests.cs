using Moq;
using Orleans;
using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;
using Outcompute.Trader.Trading.Providers.Klines;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class KlineProviderTests
    {
        [Fact]
        public async Task GetsKlines()
        {
            // arrange
            var symbol = "ABCXYZ";
            var interval = KlineInterval.Days1;
            var openTime = DateTime.UtcNow.Date;
            var items = new KlineCollection(new[]
            {
                Kline.Empty with { Symbol = symbol, Interval = interval, OpenTime = openTime },
                Kline.Empty with { Symbol = symbol, Interval = interval, OpenTime = openTime.Subtract(TimeSpan.FromDays(1)) }
            });

            var factory = Mock.Of<IGrainFactory>();
            Mock.Get(factory)
                .Setup(x => x.GetGrain<IKlineProviderReplicaGrain>($"{symbol}|{interval}", null).GetKlinesAsync())
                .ReturnsAsync(items)
                .Verifiable();

            var provider = new KlineProvider(factory);

            // act
            var result = await provider.GetKlinesAsync(symbol, interval);

            // assert
            Assert.Same(items, result);
            Mock.Get(factory).VerifyAll();
        }

        [Fact]
        public async Task SetsKline()
        {
            // arrange
            var symbol = "ABCXYZ";
            var interval = KlineInterval.Days1;
            var openTime = DateTime.UtcNow.Date;
            var item = Kline.Empty with { Symbol = symbol, Interval = interval, OpenTime = openTime };

            var factory = Mock.Of<IGrainFactory>();
            Mock.Get(factory)
                .Setup(x => x.GetGrain<IKlineProviderReplicaGrain>($"{symbol}|{interval}", null).SetKlineAsync(item))
                .Verifiable();

            var provider = new KlineProvider(factory);

            // act
            await provider.SetKlineAsync(item);

            // assert
            Mock.Get(factory).VerifyAll();
        }

        [Fact]
        public async Task GetsKline()
        {
            // arrange
            var symbol = "ABCXYZ";
            var interval = KlineInterval.Days1;
            var openTime = DateTime.UtcNow.Date;
            var item = Kline.Empty with { Symbol = symbol, Interval = interval, OpenTime = openTime };

            var factory = Mock.Of<IGrainFactory>();
            Mock.Get(factory)
                .Setup(x => x.GetGrain<IKlineProviderReplicaGrain>($"{symbol}|{interval}", null).TryGetKlineAsync(openTime))
                .ReturnsAsync(item)
                .Verifiable();

            var provider = new KlineProvider(factory);

            // act
            var result = await provider.TryGetKlineAsync(symbol, interval, openTime);

            // assert
            Assert.Same(item, result);
            Mock.Get(factory).VerifyAll();
        }

        [Fact]
        public async Task SetsKlines()
        {
            // arrange
            var symbol = "ABCXYZ";
            var interval = KlineInterval.Days1;
            var openTime = DateTime.UtcNow.Date;
            var items = new KlineCollection(new[]
            {
                Kline.Empty with { Symbol = symbol, Interval = interval, OpenTime = openTime },
                Kline.Empty with { Symbol = symbol, Interval = interval, OpenTime = openTime.Subtract(TimeSpan.FromDays(1)) }
            });

            var factory = Mock.Of<IGrainFactory>();
            Mock.Get(factory)
                .Setup(x => x.GetGrain<IKlineProviderReplicaGrain>($"{symbol}|{interval}", null).SetKlinesAsync(items))
                .Verifiable();

            var provider = new KlineProvider(factory);

            // act
            await provider.SetKlinesAsync(symbol, interval, items);

            // assert
            Mock.Get(factory).VerifyAll();
        }

        [Fact]
        public async Task TryGetLastOpenTimeAsync()
        {
            // arrange
            var symbol = Guid.NewGuid().ToString();
            var interval = KlineInterval.Days1;
            var openTime = DateTime.UtcNow.Date;

            var factory = Mock.Of<IGrainFactory>();
            Mock.Get(factory)
                .Setup(x => x.GetGrain<IKlineProviderReplicaGrain>($"{symbol}|{interval}", null).TryGetLastOpenTimeAsync())
                .ReturnsAsync(openTime)
                .Verifiable();

            var provider = new KlineProvider(factory);

            // act
            var result = await provider.TryGetLastOpenTimeAsync(symbol, interval);

            // assert
            Assert.Equal(openTime, result);
            Mock.Get(factory).VerifyAll();
        }
    }
}