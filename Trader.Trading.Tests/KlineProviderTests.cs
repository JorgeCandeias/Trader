using Microsoft.Extensions.DependencyInjection;
using Moq;
using Orleans;
using Orleans.TestingHost;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;
using Outcompute.Trader.Trading.Providers;
using Outcompute.Trader.Trading.Providers.Klines;
using Outcompute.Trader.Trading.Tests.Fixtures;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    [Collection(nameof(ClusterCollectionFixture))]
    public class KlineProviderTests
    {
        private readonly TestCluster _cluster;

        public KlineProviderTests(ClusterFixture cluster)
        {
            _cluster = cluster?.Cluster ?? throw new ArgumentNullException(nameof(cluster));
        }

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
        public async Task SetKlineAsync()
        {
            // arrange
            var symbol = Guid.NewGuid().ToString();
            var interval = KlineInterval.Days1;
            var openTime = DateTime.UtcNow.Date;
            var item = Kline.Empty with { Symbol = symbol, Interval = interval, OpenTime = openTime };

            // act
            await _cluster.ServiceProvider
                .GetRequiredService<IKlineProvider>()
                .SetKlineAsync(item);

            var result = await _cluster.ServiceProvider
                .GetRequiredService<ITradingRepository>()
                .GetKlinesAsync(symbol, interval, DateTime.MinValue, DateTime.MaxValue);

            // assert
            Assert.Collection(result, x => Assert.Equal(item, x));
        }

        [Fact]
        public async Task TryGetKlineAsync()
        {
            // arrange
            var symbol = Guid.NewGuid().ToString();
            var interval = KlineInterval.Days1;
            var openTime = DateTime.UtcNow.Date;
            var item = Kline.Empty with { Symbol = symbol, Interval = interval, OpenTime = openTime };
            await _cluster.ServiceProvider.GetRequiredService<ITradingRepository>().SetKlineAsync(item);

            // act
            var result = await _cluster.ServiceProvider
                .GetRequiredService<IKlineProvider>()
                .TryGetKlineAsync(symbol, interval, openTime)
                .ConfigureAwait(false);

            // assert
            Assert.Equal(item, result);
        }

        [Fact]
        public async Task SetKlinesAsync()
        {
            // arrange
            var symbol = Guid.NewGuid().ToString();
            var interval = KlineInterval.Days1;
            var openTime = DateTime.UtcNow.Date;
            var item = Kline.Empty with { Symbol = symbol, Interval = interval, OpenTime = openTime };

            // act
            await _cluster.ServiceProvider
                .GetRequiredService<IKlineProvider>()
                .SetKlinesAsync(symbol, interval, new[] { item });

            var result = await _cluster.ServiceProvider
                .GetRequiredService<ITradingRepository>()
                .GetKlinesAsync(symbol, interval, DateTime.MinValue, DateTime.MaxValue);

            // assert
            Assert.Collection(result, x => Assert.Equal(item, x));
        }

        [Fact]
        public async Task TryGetLastOpenTimeAsync()
        {
            // arrange
            var symbol = Guid.NewGuid().ToString();
            var interval = KlineInterval.Days1;
            var openTime = DateTime.UtcNow.Date;
            var item = Kline.Empty with { Symbol = symbol, Interval = interval, OpenTime = openTime };
            await _cluster.ServiceProvider.GetRequiredService<ITradingRepository>().SetKlineAsync(item);

            // act
            var result = await _cluster.ServiceProvider
                .GetRequiredService<IKlineProvider>()
                .TryGetLastOpenTimeAsync(symbol, interval)
                .ConfigureAwait(false);

            // assert
            Assert.Equal(openTime, result);
        }
    }
}