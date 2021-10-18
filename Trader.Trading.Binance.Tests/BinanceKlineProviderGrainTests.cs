using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.TestingHost;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Binance.Tests.Fixtures;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.Binance.Tests
{
    [Collection(nameof(ClusterCollectionFixture))]
    public class BinanceKlineProviderGrainTests
    {
        private readonly TestCluster _cluster;

        public BinanceKlineProviderGrainTests(ClusterFixture cluster)
        {
            _cluster = cluster?.Cluster ?? throw new ArgumentNullException(nameof(cluster));
        }

        [Fact]
        public async Task GetsKlines()
        {
            // arrange
            var symbol = Guid.NewGuid().ToString();
            var interval = KlineInterval.Hours1;
            var end = DateTime.UtcNow;
            var start = end.AddHours(-100);

            var klines = interval
                .Range(start, end)
                .Select(x => Kline.Empty with
                {
                    Symbol = symbol,
                    Interval = interval,
                    OpenTime = x
                })
                .ToList();

            await _cluster
                .ServiceProvider
                .GetRequiredService<ITradingRepository>()
                .SetKlinesAsync(klines);

            // act
            var result = await _cluster.GrainFactory
                .GetBinanceKlineProviderGrain(symbol, interval)
                .GetKlinesAsync(start, end);

            // assert
            Assert.NotNull(result);
        }
    }
}