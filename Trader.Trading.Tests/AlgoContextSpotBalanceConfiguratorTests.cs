using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Algorithms.Context.Configurators;
using Outcompute.Trader.Trading.Providers;

namespace Outcompute.Trader.Trading.Tests;

public class AlgoContextSpotBalanceConfiguratorTests
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

        var baseBalance = Balance.Empty with { Asset = symbol.BaseAsset, Free = 123 };
        var quoteBalance = Balance.Empty with { Asset = symbol.QuoteAsset, Free = 123 };

        var balanceProvider = Mock.Of<IBalanceProvider>();
        Mock.Get(balanceProvider)
            .Setup(x => x.TryGetBalanceAsync(symbol.BaseAsset, CancellationToken.None))
            .ReturnsAsync(baseBalance)
            .Verifiable();
        Mock.Get(balanceProvider)
            .Setup(x => x.TryGetBalanceAsync(symbol.QuoteAsset, CancellationToken.None))
            .ReturnsAsync(quoteBalance)
            .Verifiable();

        var configurator = new AlgoContextSpotBalanceConfigurator(balanceProvider);

        var provider = new ServiceCollection()
            .BuildServiceProvider();

        var context = new AlgoContext(name, provider);
        context.Symbols.AddOrUpdate(symbol);

        // act
        await configurator.ConfigureAsync(context, name);

        // assert
        Assert.Same(baseBalance, context.Data[symbol.Name].Spot.BaseAsset);
        Assert.Same(quoteBalance, context.Data[symbol.Name].Spot.QuoteAsset);
        Mock.Get(balanceProvider).VerifyAll();
    }
}