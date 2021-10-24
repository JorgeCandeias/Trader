using Microsoft.Extensions.DependencyInjection;
using Orleans.TestingHost;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers;
using Outcompute.Trader.Trading.Tests.Fixtures;
using System;
using System.Threading.Tasks;
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
        public async Task GetKlinesAsync()
        {
            // arrange
            var symbol = Guid.NewGuid().ToString();
            var interval = KlineInterval.Days1;
            var openTime = DateTime.UtcNow.Date;
            var item1 = Kline.Empty with { Symbol = symbol, Interval = interval, OpenTime = openTime };
            var item2 = Kline.Empty with { Symbol = symbol, Interval = interval, OpenTime = openTime.Subtract(TimeSpan.FromDays(1)) };
            await _cluster.ServiceProvider.GetRequiredService<ITradingRepository>().SetKlinesAsync(new[] { item1, item2 });

            // act
            var result = await _cluster.ServiceProvider
                .GetRequiredService<IKlineProvider>()
                .GetKlinesAsync(symbol, interval);

            // assert
            Assert.Equal(2, result.Count);
            Assert.Contains(item1, result);
            Assert.Contains(item2, result);
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