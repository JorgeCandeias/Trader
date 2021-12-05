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
}