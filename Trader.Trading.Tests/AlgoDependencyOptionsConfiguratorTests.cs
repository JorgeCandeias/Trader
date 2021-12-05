using Microsoft.Extensions.Options;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;

namespace Outcompute.Trader.Trading.Tests;

public class AlgoDependencyOptionsConfiguratorTests
{
    [Fact]
    public void Configures()
    {
        // arrange
        var name = "Algo1";
        var symbol1 = "ABCXYZ";
        var symbol2 = "AAAZZZ";
        var interval = KlineInterval.Days1;
        var periods = 6;
        var tradeOptions = new TraderOptions
        {
            Algos =
            {
                [name] = new AlgoOptions
                {
                    Enabled = true,
                    Symbols =
                    {
                        symbol1
                    },
                    Symbol = symbol2,
                    KlineInterval = interval,
                    KlinePeriods = periods
                }
            }
        };
        var monitor = Mock.Of<IOptionsMonitor<TraderOptions>>(x => x.CurrentValue == tradeOptions);
        var configurator = new AlgoDependencyOptionsConfigurator(monitor);

        // act
        var options = new AlgoDependencyOptions();
        configurator.Configure(options);

        // assert
        Assert.Collection(options.Symbols.OrderBy(x => x),
            x => Assert.Equal(symbol2, x),
            x => Assert.Equal(symbol1, x));
        Assert.Equal(periods, options.Klines[(symbol1, interval)]);
        Assert.Equal(periods, options.Klines[(symbol2, interval)]);
    }
}