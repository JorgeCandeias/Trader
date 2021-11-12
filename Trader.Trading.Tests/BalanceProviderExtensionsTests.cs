using Moq;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class BalanceProviderExtensionsTests
    {
        [Fact]
        public async Task GetBalanceOrZeroAsyncReturnsZero()
        {
            // arrange
            var asset = "ABC";
            var balances = Mock.Of<IBalanceProvider>();
            Mock.Get(balances)
                .Setup(x => x.TryGetBalanceAsync(asset, CancellationToken.None))
                .ReturnsAsync(() => null)
                .Verifiable();

            // act
            var result = await balances.GetBalanceOrZeroAsync(asset);

            // assert
            Mock.Get(balances).VerifyAll();
            Assert.Equal(Balance.Zero(asset), result);
        }

        [Fact]
        public async Task GetBalanceOrZeroAsyncReturnsBalance()
        {
            // arrange
            var asset = "ABC";
            var balance = Balance.Zero(asset) with { Free = 123m };
            var balances = Mock.Of<IBalanceProvider>();
            Mock.Get(balances)
                .Setup(x => x.TryGetBalanceAsync(asset, CancellationToken.None))
                .ReturnsAsync(balance)
                .Verifiable();

            // act
            var result = await balances.GetBalanceOrZeroAsync(asset);

            // assert
            Mock.Get(balances).VerifyAll();
            Assert.Equal(balance, result);
        }

        [Fact]
        public async Task GetRequiredBalanceAsyncReturnsBalance()
        {
            // arrange
            var asset = "ABC";
            var balance = Balance.Zero(asset) with { Free = 123m };
            var balances = Mock.Of<IBalanceProvider>();
            Mock.Get(balances)
                .Setup(x => x.TryGetBalanceAsync(asset, CancellationToken.None))
                .ReturnsAsync(balance)
                .Verifiable();

            // act
            var result = await balances.GetRequiredBalanceAsync(asset);

            // assert
            Mock.Get(balances).VerifyAll();
            Assert.Equal(balance, result);
        }

        [Fact]
        public async Task GetRequiredBalanceAsyncThrows()
        {
            // arrange
            var asset = "ABC";
            var balances = Mock.Of<IBalanceProvider>();
            Mock.Get(balances)
                .Setup(x => x.TryGetBalanceAsync(asset, CancellationToken.None))
                .ReturnsAsync(() => null)
                .Verifiable();

            // act
            async Task Test() => await balances!.GetRequiredBalanceAsync(asset!);

            // assert
            await Assert.ThrowsAsync<KeyNotFoundException>(Test);
            Mock.Get(balances).VerifyAll();
        }
    }
}