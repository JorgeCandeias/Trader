using Orleans;
using Outcompute.Trader.Tests.Fakes;
using Outcompute.Trader.Trading.Binance.Providers.MarketData;

namespace Outcompute.Trader.Trading.Binance.Tests;

public class BinanceMarketDataReadynessGrainTests
{
    [Fact]
    public async Task IsReadySignalsTrue()
    {
        // arrange
        var state = false;
        var factory = Mock.Of<IGrainFactory>();
        Mock.Get(factory)
            .Setup(x => x.GetGrain<IBinanceMarketDataGrain>(Guid.Empty, null).IsReadyAsync())
            .Returns(() => Task.FromResult(state))
            .Verifiable();

        var timers = new FakeTimerRegistry();

        var grain = new BinanceMarketDataReadynessGrain(factory, timers);

        // act
        await grain.OnActivateAsync();

        // assert timer was created
        var timer = Assert.Single(timers.Entries);
        Assert.Same(grain, timer.Grain);
        Assert.Equal(TimeSpan.FromSeconds(1), timer.DueTime);
        Assert.Equal(TimeSpan.FromSeconds(1), timer.Period);

        // act - get ready state
        var ready = await grain.IsReadyAsync();

        // assert current ready state is false
        Assert.Equal(state, ready);

        // arrange dependant state
        state = true;

        // act - simulate tick
        await timer.AsyncCallback(timer.State);

        // act - get ready state
        ready = await grain.IsReadyAsync();

        // assert current ready state is true
        Assert.Equal(state, ready);

        // act - simulate deactivation
        await grain.OnDeactivateAsync();

        // assert everything was called
        Mock.Get(factory).VerifyAll();
    }
}