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
}