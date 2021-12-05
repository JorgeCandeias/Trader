using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Trading.Algorithms.Context;

namespace Outcompute.Trader.Trading.Tests;

public class AlgoContextFactoryTests
{
    [Fact]
    public void CreatesContext()
    {
        // arrange
        var name = "Algo1";

        var local = new AlgoContextLocal();

        var provider = new ServiceCollection()
            .AddScoped<IAlgoContextLocal>(_ => local)
            .BuildServiceProvider();

        var factory = new AlgoContextFactory(provider);

        // act
        var result = factory.Create(name);

        // assert
        Assert.NotNull(result);
        Assert.Equal(name, result.Name);
        Assert.Same(provider, result.ServiceProvider);
        Assert.Same(result, local.Context);
    }
}