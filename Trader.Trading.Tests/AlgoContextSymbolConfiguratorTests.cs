using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Algorithms.Context.Configurators;
using Outcompute.Trader.Trading.Providers;

namespace Outcompute.Trader.Trading.Tests;

public class AlgoContextSymbolConfiguratorTests
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

        var options = new AlgoOptions
        {
            Symbols =
            {
                symbol.Name
            },
            Symbol = symbol.Name
        };

        var monitor = Mock.Of<IOptionsMonitor<AlgoOptions>>(x => x.Get(name) == options);

        var exchange = Mock.Of<IExchangeInfoProvider>();
        Mock.Get(exchange)
            .Setup(x => x.TryGetSymbol(symbol.Name))
            .Returns(symbol)
            .Verifiable();

        var configurator = new AlgoContextSymbolConfigurator(monitor, exchange);

        var provider = new ServiceCollection()
            .BuildServiceProvider();

        var context = new AlgoContext(name, provider);
        context.Symbols.AddOrUpdate(symbol);

        // act
        await configurator.ConfigureAsync(context, name);

        // assert
        Assert.Collection(context.Symbols, x => Assert.Same(symbol, x));
        Assert.Same(symbol, context.Data[symbol.Name].Symbol);
        Assert.Same(symbol, context.Symbol);
        Mock.Get(exchange).VerifyAll();
    }
}