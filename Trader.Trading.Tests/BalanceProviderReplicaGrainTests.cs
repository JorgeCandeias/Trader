using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Runtime;
using Outcompute.Trader.Models;
using Outcompute.Trader.Tests.Fakes;
using Outcompute.Trader.Trading.Providers.Balances;

namespace Outcompute.Trader.Trading.Tests;

public class BalanceProviderReplicaGrainTests
{
    [Fact]
    public async Task Activates()
    {
        // arrange
        var asset = "Asset1";
        var version = Guid.NewGuid();
        var value = Balance.Empty with { Asset = asset, Free = 123 };

        var options = new ReactiveOptions
        {
            ReactiveRecoveryDelay = TimeSpan.FromSeconds(10)
        };

        var context = Mock.Of<IGrainActivationContext>(x => x.GrainIdentity.PrimaryKeyString == asset);

        var factory = Mock.Of<IGrainFactory>();
        Mock.Get(factory)
            .Setup(x => x.GetGrain<IBalanceProviderGrain>(asset, null).GetBalanceAsync())
            .ReturnsAsync(new ReactiveResult(version, value))
            .Verifiable();

        var lifetime = Mock.Of<IHostApplicationLifetime>();
        var timers = new FakeTimerRegistry();

        var grain = new BalanceProviderReplicaGrain(Options.Create(options), context, factory, lifetime, timers);

        // act
        await grain.OnActivateAsync();

        // assert
        var entry = Assert.Single(timers.Entries);
        Assert.Equal(options.ReactiveRecoveryDelay, entry.DueTime);
        Assert.Equal(options.ReactiveRecoveryDelay, entry.Period);

        // deactivate
        await grain.OnDeactivateAsync();
    }

    [Fact]
    public async Task TryGetBalance()
    {
        // arrange
        var asset = "Asset1";
        var version = Guid.NewGuid();
        var balance = Balance.Empty with { Asset = asset, Free = 123 };

        var options = new ReactiveOptions
        {
            ReactiveRecoveryDelay = TimeSpan.FromSeconds(10)
        };

        var context = Mock.Of<IGrainActivationContext>(x => x.GrainIdentity.PrimaryKeyString == asset);

        var factory = Mock.Of<IGrainFactory>();
        Mock.Get(factory)
            .Setup(x => x.GetGrain<IBalanceProviderGrain>(asset, null).GetBalanceAsync())
            .ReturnsAsync(new ReactiveResult(version, balance))
            .Verifiable();

        var lifetime = Mock.Of<IHostApplicationLifetime>();
        var timers = new FakeTimerRegistry();

        var grain = new BalanceProviderReplicaGrain(Options.Create(options), context, factory, lifetime, timers);

        // activate
        await grain.OnActivateAsync();

        // act
        var result = await grain.TryGetBalanceAsync();

        // assert
        Assert.Same(balance, result);

        // deactivate
        await grain.OnDeactivateAsync();
    }

    [Fact]
    public async Task Polls()
    {
        // arrange
        var asset = "Asset1";

        var version1 = Guid.NewGuid();
        var balance1 = Balance.Empty with { Asset = asset, Free = 123 };

        var version2 = Guid.NewGuid();
        var balance2 = Balance.Empty with { Asset = asset, Free = 234 };

        var options = new ReactiveOptions
        {
            ReactiveRecoveryDelay = TimeSpan.FromSeconds(10)
        };

        var context = Mock.Of<IGrainActivationContext>(x => x.GrainIdentity.PrimaryKeyString == asset);

        var factory = Mock.Of<IGrainFactory>();
        Mock.Get(factory)
            .Setup(x => x.GetGrain<IBalanceProviderGrain>(asset, null).GetBalanceAsync())
            .ReturnsAsync(new ReactiveResult(version1, balance1))
            .Verifiable();
        Mock.Get(factory)
            .Setup(x => x.GetGrain<IBalanceProviderGrain>(asset, null).TryWaitForBalanceAsync(version1))
            .ReturnsAsync(new ReactiveResult(version2, balance2))
            .Verifiable();

        using var cancellation = new CancellationTokenSource();

        var lifetime = Mock.Of<IHostApplicationLifetime>();
        Mock.Get(lifetime)
            .Setup(x => x.ApplicationStopping)
            .Returns(cancellation.Token)
            .Verifiable();

        var timers = new FakeTimerRegistry();

        var grain = new BalanceProviderReplicaGrain(Options.Create(options), context, factory, lifetime, timers);

        // activate
        await grain.OnActivateAsync();

        // act
        var result1 = await grain.TryGetBalanceAsync();
        await timers.Entries.Single().ExecuteAsync();
        await Task.Delay(100);
        var result2 = await grain.TryGetBalanceAsync();
        cancellation.Cancel();

        // assert
        Assert.Same(balance1, result1);
        Assert.Same(balance2, result2);
        Mock.Get(lifetime).VerifyAll();

        // deactivate
        await grain.OnDeactivateAsync();
    }
}