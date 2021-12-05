using Microsoft.Extensions.Options;
using Outcompute.Trader.Trading.Algorithms;

namespace Outcompute.Trader.Trading.Tests;

public class AlgoDependencyResolverTests
{
    [Fact]
    public void ExposesOptions()
    {
        // arrange
        var options = new AlgoDependencyOptions();
        var monitor = Mock.Of<IOptionsMonitor<AlgoDependencyOptions>>(x => x.CurrentValue == options);
        var resolver = new AlgoDependencyResolver(monitor);

        // act
        var symbols = resolver.Symbols;
        var klines = resolver.Klines;

        // assert
        Assert.Same(options.Symbols, symbols);
        Assert.Same(options.Klines, klines);
    }
}