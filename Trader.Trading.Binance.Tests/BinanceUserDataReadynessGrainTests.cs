using Orleans;
using Outcompute.Trader.Tests.Fakes;
using Outcompute.Trader.Trading.Binance.Providers.UserData;

namespace Outcompute.Trader.Trading.Binance.Tests;

public sealed class BinanceUserDataReadynessGrainTests
{
    [Fact]
    public async Task IsReadyAsync()
    {
        // arrange
        var factory = Mock.Of<IGrainFactory>();
        Mock.Get(factory)
            .SetupSequence(x => x.GetGrain<IBinanceUserDataGrain>(Guid.Empty, null).IsReadyAsync())
            .ReturnsAsync(false)
            .ReturnsAsync(true);

        var timers = new FakeTimerRegistry();

        var grain = new BinanceUserDataReadynessGrain(factory, timers);

        // act - activation call
        await grain.OnActivateAsync();
        var result = await grain.IsReadyAsync();

        // assert
        Assert.False(result);
        var timer = Assert.Single(timers.Entries);

        // act - trigger the timer
        await timer.ExecuteAsync();
        result = await grain.IsReadyAsync();

        // assert
        Assert.True(result);
    }
}