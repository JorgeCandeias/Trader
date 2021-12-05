using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Algorithms.Context.Configurators;
using Outcompute.Trader.Trading.Providers;

namespace Outcompute.Trader.Trading.Tests;

public class AlgoContextTradesConfiguratorTests
{
    [Fact]
    public async Task Configures()
    {
        // arrange
        var name = "Algo1";

        var symbol = Symbol.Empty with
        {
            Name = "ABCXYZ"
        };

        var trades = new TradeCollection(Array.Empty<AccountTrade>());

        var tradeProvider = Mock.Of<ITradeProvider>();
        Mock.Get(tradeProvider)
            .Setup(x => x.GetTradesAsync(symbol.Name, CancellationToken.None))
            .ReturnsAsync(trades)
            .Verifiable();

        var configurator = new AlgoContextTradesConfigurator(tradeProvider);

        var provider = new ServiceCollection()
            .BuildServiceProvider();

        var context = new AlgoContext(name, provider);
        context.Symbols.AddOrUpdate(symbol);

        // act
        await configurator.ConfigureAsync(context, name);

        // assert
        Assert.Same(trades, context.Data[symbol.Name].Trades);
        Mock.Get(tradeProvider).VerifyAll();
    }
}