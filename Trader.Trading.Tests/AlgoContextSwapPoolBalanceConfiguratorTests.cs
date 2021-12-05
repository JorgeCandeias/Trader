using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Algorithms.Context.Configurators;
using Outcompute.Trader.Trading.Providers;

namespace Outcompute.Trader.Trading.Tests;

public class AlgoContextSwapPoolBalanceConfiguratorTests
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

        var baseBalance = SwapPoolAssetBalance.Empty with { Asset = symbol.BaseAsset, Total = 123 };
        var quoteBalance = SwapPoolAssetBalance.Empty with { Asset = symbol.QuoteAsset, Total = 123 };

        var swapPoolProvider = Mock.Of<ISwapPoolProvider>();
        Mock.Get(swapPoolProvider)
            .Setup(x => x.GetBalanceAsync(symbol.BaseAsset, CancellationToken.None))
            .ReturnsAsync(baseBalance)
            .Verifiable();
        Mock.Get(swapPoolProvider)
            .Setup(x => x.GetBalanceAsync(symbol.QuoteAsset, CancellationToken.None))
            .ReturnsAsync(quoteBalance)
            .Verifiable();

        var configurator = new AlgoContextSwapPoolBalanceConfigurator(swapPoolProvider);

        var provider = new ServiceCollection()
            .BuildServiceProvider();

        var context = new AlgoContext(name, provider);
        context.Symbols.AddOrUpdate(symbol);

        // act
        await configurator.ConfigureAsync(context, name);

        // assert
        Assert.Same(baseBalance, context.Data[symbol.Name].SwapPools.BaseAsset);
        Assert.Same(quoteBalance, context.Data[symbol.Name].SwapPools.QuoteAsset);
        Mock.Get(swapPoolProvider).VerifyAll();
    }
}