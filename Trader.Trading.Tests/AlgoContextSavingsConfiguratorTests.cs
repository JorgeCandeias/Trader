using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Algorithms.Context.Configurators;
using Outcompute.Trader.Trading.Providers;

namespace Outcompute.Trader.Trading.Tests;

public class AlgoContextSavingsConfiguratorTests
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

        var baseBalance = SavingsBalance.Empty with { Asset = symbol.BaseAsset, FreeAmount = 123 };
        var quoteBalance = SavingsBalance.Empty with { Asset = symbol.QuoteAsset, FreeAmount = 123 };

        var savingsProvider = Mock.Of<ISavingsProvider>();
        Mock.Get(savingsProvider)
            .Setup(x => x.TryGetBalanceAsync(symbol.BaseAsset, CancellationToken.None))
            .ReturnsAsync(baseBalance)
            .Verifiable();
        Mock.Get(savingsProvider)
            .Setup(x => x.TryGetBalanceAsync(symbol.QuoteAsset, CancellationToken.None))
            .ReturnsAsync(quoteBalance)
            .Verifiable();

        var configurator = new AlgoContextSavingsConfigurator(savingsProvider);

        var provider = new ServiceCollection()
            .BuildServiceProvider();

        var context = new AlgoContext(name, provider);
        context.Symbols.AddOrUpdate(symbol);

        // act
        await configurator.ConfigureAsync(context, name);

        // assert
        Assert.Same(baseBalance, context.Data[symbol.Name].Savings.BaseAsset);
        Assert.Same(quoteBalance, context.Data[symbol.Name].Savings.QuoteAsset);
        Mock.Get(savingsProvider).VerifyAll();
    }
}