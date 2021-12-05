using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Algorithms.Context.Configurators;
using Outcompute.Trader.Trading.Providers;

namespace Outcompute.Trader.Trading.Tests;

public class AlgoContextExchangeInfoConfiguratorTests
{
    [Fact]
    public async Task Configures()
    {
        // arrange
        var name = "Algo1";

        var info = ExchangeInfo.Empty with { ServerTime = DateTime.UtcNow };

        var exchange = Mock.Of<IExchangeInfoProvider>(x => x.GetExchangeInfo() == info);

        var configurator = new AlgoContextExchangeInfoConfigurator(exchange);

        var provider = new ServiceCollection()
            .BuildServiceProvider();

        var context = new AlgoContext(name, provider);

        // act
        await configurator.ConfigureAsync(context, name);

        // assert
        Assert.Same(info, context.Exchange);
    }
}