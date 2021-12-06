using Microsoft.Extensions.Options;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Algorithms.Context.Configurators;
using Outcompute.Trader.Trading.Providers;

namespace Outcompute.Trader.Trading.Tests;

public class AlgoContextKlinesConfiguratorTests
{
    [Fact]
    public async Task Configures()
    {
        // arrange
        var name = "Algo1";
        var interval = KlineInterval.Days1;
        var time = DateTime.UtcNow;
        var periods = 6;

        var symbol = Symbol.Empty with
        {
            Name = "ABCXYZ"
        };

        var options = new AlgoOptions
        {
            StartTime = DateTime.UtcNow,
            KlineInterval = interval,
            KlinePeriods = periods
        };

        var klines = ImmutableSortedSet.Create(Kline.KeyComparer, Kline.Empty with { Symbol = symbol.Name, Interval = interval, OpenTime = time.AddSeconds(-1) });

        var monitor = Mock.Of<IOptionsMonitor<AlgoOptions>>(x => x.Get(name) == options);

        var klineProvider = Mock.Of<IKlineProvider>();
        Mock.Get(klineProvider)
            .Setup(x => x.GetKlinesAsync(symbol.Name, interval, time, periods, CancellationToken.None))
            .ReturnsAsync(klines)
            .Verifiable();

        var configurator = new AlgoContextKlinesConfigurator(monitor, klineProvider);

        var provider = new ServiceCollection()
            .BuildServiceProvider();

        var context = new AlgoContext(name, provider);
        context.Symbols.AddOrUpdate(symbol);
        context.TickTime = time;

        // act
        await configurator.ConfigureAsync(context, name);

        // assert
        Assert.Equal(interval, context.KlineInterval);
        Assert.Equal(periods, context.KlinePeriods);
        Assert.Same(klines, context.Data[symbol.Name].Klines);
        Mock.Get(klineProvider).VerifyAll();
    }
}