using Microsoft.Extensions.Options;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Algorithms.Context.Configurators;
using Outcompute.Trader.Trading.Algorithms.Positions;

namespace Outcompute.Trader.Trading.Tests;

public class AlgoContextAutoPositionsConfiguratorTests
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

        var options = new AlgoOptions
        {
            StartTime = DateTime.UtcNow
        };

        var orders = ImmutableSortedSet.Create(OrderQueryResult.KeyComparer, OrderQueryResult.Empty with { OrderId = 123 });

        var trades = ImmutableSortedSet.Create(AccountTrade.KeyComparer, AccountTrade.Empty with { Id = 123 });

        var position = new AutoPosition();

        var monitor = Mock.Of<IOptionsMonitor<AlgoOptions>>(x => x.Get(name) == options);

        var logger = NullLogger<AlgoContextAutoPositionsConfigurator>.Instance;

        var resolver = Mock.Of<IAutoPositionResolver>();
        Mock.Get(resolver)
            .Setup(x => x.Resolve(symbol, orders, trades, options.StartTime))
            .Returns(position)
            .Verifiable();

        var configurator = new AlgoContextAutoPositionsConfigurator(monitor, logger, resolver);

        var provider = new ServiceCollection()
            .BuildServiceProvider();

        var context = new AlgoContext(name, provider);
        context.Symbols.AddOrUpdate(symbol);

        var data = context.Data["ABCXYZ"];
        data.Orders.Filled = orders;
        data.Trades = trades;

        // act
        await configurator.ConfigureAsync(context, name);

        // assert
        Assert.Same(position, data.AutoPosition);
    }
}