using Microsoft.Extensions.Logging.Abstractions;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Commands.EnsureSpotBalance;
using Outcompute.Trader.Trading.Providers;

namespace Outcompute.Trader.Trading.Tests;

public class EnsureSpotBalanceExecutorTests
{
    [Fact]
    public async Task ExecuteReturnsSuccessOnAvailableSpotAmount()
    {
        // arrange
        var logger = NullLogger<EnsureSpotBalanceExecutor>.Instance;
        var asset = "ABC";
        var balance = Balance.Empty with { Asset = asset, Free = 1000 };

        var balances = Mock.Of<IBalanceProvider>();
        Mock.Get(balances)
            .Setup(x => x.TryGetBalanceAsync(asset, CancellationToken.None))
            .ReturnsAsync(balance)
            .Verifiable();

        var savings = Mock.Of<ISavingsProvider>();

        var executor = new EnsureSpotBalanceExecutor(logger, balances, savings);

        var context = Mock.Of<IAlgoContext>();
        var command = new EnsureSpotBalanceCommand(asset, 1000, true, true);

        // act
        var result = await executor.ExecuteAsync(context, command);

        // assert
        Assert.True(result.Success);
        Assert.Equal(0, result.Redeemed);
        Mock.Get(balances).VerifyAll();
    }

    [Fact]
    public async Task ExecuteRedeemsAllFromSavings()
    {
        // arrange
        var logger = NullLogger<EnsureSpotBalanceExecutor>.Instance;
        var asset = "ABC";
        var balance = Balance.Empty with { Asset = asset, Free = 0 };

        var balances = Mock.Of<IBalanceProvider>();
        Mock.Get(balances)
            .Setup(x => x.TryGetBalanceAsync(asset, CancellationToken.None))
            .ReturnsAsync(balance)
            .Verifiable();

        var savings = Mock.Of<ISavingsProvider>();
        Mock.Get(savings)
            .Setup(x => x.RedeemAsync(asset, 1000, CancellationToken.None))
            .ReturnsAsync(new RedeemSavingsEvent(true, 1000))
            .Verifiable();

        var executor = new EnsureSpotBalanceExecutor(logger, balances, savings);

        var command = new EnsureSpotBalanceCommand(asset, 1000, true, true);

        var context = Mock.Of<IAlgoContext>();

        // act
        var result = await executor.ExecuteAsync(context, command);

        // assert
        Assert.True(result.Success);
        Assert.Equal(1000, result.Redeemed);
        Mock.Get(balances).VerifyAll();
        Mock.Get(savings).VerifyAll();
        Mock.Get(context).VerifyAll();
    }
}