using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Orleans.Runtime;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers.Balances;

namespace Outcompute.Trader.Trading.Tests;

public class BalanceProviderGrainTests
{
    [Fact]
    public async Task Activates()
    {
        // arrange
        var asset = "ABC";
        var options = new ReactiveOptions();
        var context = Mock.Of<IGrainActivationContext>(x => x.GrainIdentity.PrimaryKeyString == asset);

        var balance = Balance.Empty with { Asset = asset, Free = 123 };

        var repository = Mock.Of<ITradingRepository>();
        Mock.Get(repository)
            .Setup(x => x.TryGetBalanceAsync(asset, CancellationToken.None))
            .ReturnsAsync(balance)
            .Verifiable();

        var lifetime = Mock.Of<IHostApplicationLifetime>();
        var grain = new BalanceProviderGrain(Options.Create(options), context, repository, lifetime);

        // act
        await grain.OnActivateAsync();

        // assert
        Mock.Get(repository).VerifyAll();
    }

    [Fact]
    public async Task TryGetBalance()
    {
        // arrange
        var asset = "ABC";
        var options = new ReactiveOptions();
        var context = Mock.Of<IGrainActivationContext>(x => x.GrainIdentity.PrimaryKeyString == asset);

        var balance = Balance.Empty with { Asset = asset, Free = 123 };

        var repository = Mock.Of<ITradingRepository>();
        Mock.Get(repository)
            .Setup(x => x.TryGetBalanceAsync(asset, CancellationToken.None))
            .ReturnsAsync(balance)
            .Verifiable();

        var lifetime = Mock.Of<IHostApplicationLifetime>();
        var grain = new BalanceProviderGrain(Options.Create(options), context, repository, lifetime);

        // activate
        await grain.OnActivateAsync();

        // act
        var result = await grain.TryGetBalanceAsync();

        // assert
        Assert.Same(result, balance);
        Mock.Get(repository).VerifyAll();
    }

    [Fact]
    public async Task SetBalance()
    {
        // arrange
        var asset = "ABC";
        var options = new ReactiveOptions();
        var context = Mock.Of<IGrainActivationContext>(x => x.GrainIdentity.PrimaryKeyString == asset);

        var balance1 = Balance.Empty with { Asset = asset, Free = 123 };
        var balance2 = Balance.Empty with { Asset = asset, Free = 234 };

        var repository = Mock.Of<ITradingRepository>();
        Mock.Get(repository)
            .Setup(x => x.TryGetBalanceAsync(asset, CancellationToken.None))
            .ReturnsAsync(balance1)
            .Verifiable();

        var lifetime = Mock.Of<IHostApplicationLifetime>();
        var grain = new BalanceProviderGrain(Options.Create(options), context, repository, lifetime);

        // activate
        await grain.OnActivateAsync();

        // act
        await grain.SetBalanceAsync(balance2);
        var result = await grain.TryGetBalanceAsync();

        // assert
        Assert.Same(result, balance2);
        Mock.Get(repository).VerifyAll();
    }

    [Fact]
    public async Task ReactiveBalance()
    {
        // arrange
        var asset = "ABC";
        var options = new ReactiveOptions();
        var context = Mock.Of<IGrainActivationContext>(x => x.GrainIdentity.PrimaryKeyString == asset);

        var balance1 = Balance.Empty with { Asset = asset, Free = 123 };
        var balance2 = Balance.Empty with { Asset = asset, Free = 234 };

        var repository = Mock.Of<ITradingRepository>();
        Mock.Get(repository)
            .Setup(x => x.TryGetBalanceAsync(asset, CancellationToken.None))
            .ReturnsAsync(balance1)
            .Verifiable();

        var lifetime = Mock.Of<IHostApplicationLifetime>();
        var grain = new BalanceProviderGrain(Options.Create(options), context, repository, lifetime);

        // activate
        await grain.OnActivateAsync();

        // act
        var result1 = await grain.GetBalanceAsync();
        var result2Task = grain.TryWaitForBalanceAsync(result1.Version);
        await grain.SetBalanceAsync(balance2);
        var result2 = await result2Task;

        // assert
        Assert.Same(balance1, result1.Value);
        Assert.Same(balance2, result2.Value.Value);
        Assert.NotEqual(result1.Version, result2.Value.Version);
        Mock.Get(repository).VerifyAll();
    }
}