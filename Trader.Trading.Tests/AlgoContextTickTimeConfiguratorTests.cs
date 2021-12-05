using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Algorithms.Context.Configurators;

namespace Outcompute.Trader.Trading.Tests;

public class AlgoContextTickTimeConfiguratorTests
{
    [Fact]
    public async Task Configures()
    {
        // arrange
        var name = "Algo1";

        var time = DateTime.UtcNow;

        var clock = Mock.Of<ISystemClock>(x => x.UtcNow == time);

        var configurator = new AlgoContextTickTimeConfigurator(clock);

        var provider = new ServiceCollection()
            .BuildServiceProvider();

        var context = new AlgoContext(name, provider);

        // act
        await configurator.ConfigureAsync(context, name);

        // assert
        Assert.Equal(time, context.TickTime);
        Mock.Get(clock).VerifyAll();
    }
}