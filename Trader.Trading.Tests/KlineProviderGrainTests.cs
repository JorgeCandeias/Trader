using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers.Klines;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class KlineProviderGrainTests
    {
        [Fact]
        public async Task SetsAndGetsKline()
        {
            // arrange
            var symbol = "ABCXYZ";
            var interval = KlineInterval.Days1;
            var options = Options.Create(new KlineProviderOptions());
            var reactive = Options.Create(new ReactiveOptions());
            var repository = Mock.Of<ITradingRepository>();
            var clock = Mock.Of<ISystemClock>();
            var lifetime = Mock.Of<IHostApplicationLifetime>();
            var grain = new KlineProviderGrain(options, reactive, repository, clock, lifetime)
            {
                _symbol = symbol,
                _interval = interval
            };
            var item = Kline.Empty with
            {
                Symbol = symbol,
                Interval = interval,
                OpenTime = DateTime.UtcNow.Date
            };

            // act
            await grain.SetKlineAsync(item);
            var result = await grain.TryGetKlineAsync(item.OpenTime);

            // assert
            Assert.NotNull(result);
            Assert.Equal(item.Symbol, result!.Symbol);
            Assert.Equal(item.Interval, result.Interval);
            Assert.Equal(item.OpenTime, result.OpenTime);
        }

        [Fact]
        public async Task CompletesPromises()
        {
            // arrange
            var symbol = "ABCXYZ";
            var interval = KlineInterval.Days1;
            var options = Options.Create(new KlineProviderOptions());
            var reactive = Options.Create(new ReactiveOptions());
            var repository = Mock.Of<ITradingRepository>();
            var clock = Mock.Of<ISystemClock>();
            var lifetime = Mock.Of<IHostApplicationLifetime>();
            var grain = new KlineProviderGrain(options, reactive, repository, clock, lifetime)
            {
                _symbol = symbol,
                _interval = interval
            };
            var item = Kline.Empty with
            {
                Symbol = symbol,
                Interval = interval,
                OpenTime = DateTime.UtcNow.Date
            };

            // act
            var current = await grain.GetKlinesAsync();
            var promise = grain.TryWaitForKlinesAsync(current.Version, current.Serial + 1);
            await grain.SetKlineAsync(item);
            var result = await promise;

            // assert
            Assert.True(result.HasValue);
            Assert.Collection(result.Value.Items, x => Assert.Equal(item, x));
            Assert.Equal(current.Serial + 1, result.Value.Serial);
            Assert.Equal(current.Version, result.Value.Version);
        }
    }
}