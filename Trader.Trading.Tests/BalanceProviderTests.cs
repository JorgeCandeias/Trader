using Microsoft.Extensions.DependencyInjection;
using Orleans.TestingHost;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers;
using Outcompute.Trader.Trading.Tests.Fixtures;
using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    [Collection(nameof(ClusterCollectionFixture))]
    public class BalanceProviderTests
    {
        private readonly TestCluster _cluster;

        public BalanceProviderTests(ClusterFixture cluster)
        {
            _cluster = cluster?.Cluster ?? throw new ArgumentNullException(nameof(cluster));
        }

        [Fact]
        public async Task SetsAndGetsAccountInfoBalances()
        {
            // arrange
            var asset1 = Guid.NewGuid().ToString();
            var asset2 = Guid.NewGuid().ToString();
            var balance1 = AccountBalance.Empty with { Asset = asset1, Free = 123m };
            var balance2 = AccountBalance.Empty with { Asset = asset2, Free = 234m };
            var info = AccountInfo.Empty with { AccountType = AccountType.Spot, Balances = ImmutableList.Create(balance1, balance2) };
            var provider = _cluster.ServiceProvider.GetRequiredService<IBalanceProvider>();

            // act
            await provider.SetBalancesAsync(info);
            var result1 = await provider.TryGetBalanceAsync(asset1);
            var result2 = await provider.TryGetBalanceAsync(asset2);
            var result3 = await provider.TryGetBalanceAsync(Guid.NewGuid().ToString());

            // assert
            Assert.NotNull(result1);
            Assert.Equal(asset1, result1!.Asset);
            Assert.Equal(123m, result1.Free);

            Assert.NotNull(result2);
            Assert.Equal(asset2, result2!.Asset);
            Assert.Equal(234m, result2.Free);

            Assert.Null(result3);
        }

        [Fact]
        public async Task SetsAndGetsBalances()
        {
            // arrange
            var asset1 = Guid.NewGuid().ToString();
            var asset2 = Guid.NewGuid().ToString();
            var balance1 = Balance.Empty with { Asset = asset1, Free = 123m };
            var balance2 = Balance.Empty with { Asset = asset2, Free = 234m };
            var balances = new[] { balance1, balance2 };
            var provider = _cluster.ServiceProvider.GetRequiredService<IBalanceProvider>();

            // act
            await provider.SetBalancesAsync(balances);
            var result1 = await provider.TryGetBalanceAsync(asset1);
            var result2 = await provider.TryGetBalanceAsync(asset2);
            var result3 = await provider.TryGetBalanceAsync(Guid.NewGuid().ToString());

            // assert
            Assert.NotNull(result1);
            Assert.Equal(asset1, result1!.Asset);
            Assert.Equal(123m, result1.Free);

            Assert.NotNull(result2);
            Assert.Equal(asset2, result2!.Asset);
            Assert.Equal(234m, result2.Free);

            Assert.Null(result3);
        }
    }
}