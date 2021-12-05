using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Algorithms.Context.Configurators;
using Outcompute.Trader.Trading.Providers;

namespace Outcompute.Trader.Trading.Tests;

public class AlgoContextTickerConfiguratorTests
{
    [Fact]
    public async Task Configures()
    {
        // arrange
        var name = "Algo1";

        var symbol = Symbol.Empty with
        {
            Name = "ABCXYZ",
            BaseAsset = "ABC",
            QuoteAsset = "XYZ"
        };

        var ticker = MiniTicker.Empty with { Symbol = symbol.Name, ClosePrice = 123 };

        var tickerProvider = Mock.Of<ITickerProvider>();
        Mock.Get(tickerProvider)
            .Setup(x => x.TryGetTickerAsync(symbol.Name, CancellationToken.None))
            .ReturnsAsync(ticker)
            .Verifiable();

        var configurator = new AlgoContextTickerConfigurator(tickerProvider);

        var provider = new ServiceCollection()
            .BuildServiceProvider();

        var context = new AlgoContext(name, provider);
        context.Symbols.AddOrUpdate(symbol);

        // act
        await configurator.ConfigureAsync(context, name);

        // assert
        Assert.Same(ticker, context.Data[symbol.Name].Ticker);
        Mock.Get(tickerProvider).VerifyAll();
    }
}