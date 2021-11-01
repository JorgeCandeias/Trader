using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Commands.RedeemSavings;
using Outcompute.Trader.Trading.Providers;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class RedeemSavingsExecutorTests
    {
        [Fact]
        public async Task ExecuteFailsOnNotRedeemable()
        {
            // arrange
            var asset = "ABC";
            var amount = 123m;
            var position = SavingsPosition.Zero(asset) with { CanRedeem = false };

            var logger = NullLogger<RedeemSavingsExecutor>.Instance;

            var savings = Mock.Of<ISavingsProvider>();
            Mock.Get(savings)
                .Setup(x => x.TryGetPositionAsync(asset, CancellationToken.None))
                .ReturnsAsync(position)
                .Verifiable();

            var executor = new RedeemSavingsExecutor(logger, savings);
            var context = AlgoContext.Empty;
            var command = new RedeemSavingsCommand(asset, amount);

            // act
            var result = await executor.ExecuteAsync(context, command, CancellationToken.None);

            // assert
            Mock.Get(savings).VerifyAll();
            Assert.False(result.Success);
            Assert.Equal(0m, result.Redeemed);
        }

        [Fact]
        public async Task ExecuteFailsOnActiveRedemption()
        {
            // arrange
            var asset = "ABC";
            var amount = 123m;
            var position = SavingsPosition.Zero(asset) with { CanRedeem = true, RedeemingAmount = 1m };

            var logger = NullLogger<RedeemSavingsExecutor>.Instance;

            var savings = Mock.Of<ISavingsProvider>();
            Mock.Get(savings)
                .Setup(x => x.TryGetPositionAsync(asset, CancellationToken.None))
                .ReturnsAsync(position)
                .Verifiable();

            var executor = new RedeemSavingsExecutor(logger, savings);
            var context = AlgoContext.Empty;
            var command = new RedeemSavingsCommand(asset, amount);

            // act
            var result = await executor.ExecuteAsync(context, command, CancellationToken.None);

            // assert
            Mock.Get(savings).VerifyAll();
            Assert.False(result.Success);
            Assert.Equal(0m, result.Redeemed);
        }

        [Fact]
        public async Task ExecuteFailsOnNotEnoughSavings()
        {
            // arrange
            var asset = "ABC";
            var amount = 123m;
            var position = SavingsPosition.Zero(asset) with { CanRedeem = true, RedeemingAmount = 0m, FreeAmount = 100m };

            var logger = NullLogger<RedeemSavingsExecutor>.Instance;

            var savings = Mock.Of<ISavingsProvider>();
            Mock.Get(savings)
                .Setup(x => x.TryGetPositionAsync(asset, CancellationToken.None))
                .ReturnsAsync(position)
                .Verifiable();

            var executor = new RedeemSavingsExecutor(logger, savings);
            var context = AlgoContext.Empty;
            var command = new RedeemSavingsCommand(asset, amount);

            // act
            var result = await executor.ExecuteAsync(context, command, CancellationToken.None);

            // assert
            Mock.Get(savings).VerifyAll();
            Assert.False(result.Success);
            Assert.Equal(0m, result.Redeemed);
        }

        [Fact]
        public async Task ExecuteFailsOnNotEnoughQuota()
        {
            // arrange
            var asset = "ABC";
            var amount = 123m;
            var productId = "P1";
            var type = SavingsRedemptionType.Fast;
            var position = SavingsPosition.Zero(asset) with { CanRedeem = true, RedeemingAmount = 0m, FreeAmount = 200m, ProductId = productId };
            var quota = SavingsQuota.Zero(asset) with { LeftQuota = 1000m };

            var logger = NullLogger<RedeemSavingsExecutor>.Instance;

            var savings = Mock.Of<ISavingsProvider>();
            Mock.Get(savings)
                .Setup(x => x.TryGetPositionAsync(asset, CancellationToken.None))
                .ReturnsAsync(position)
                .Verifiable();
            Mock.Get(savings)
                .Setup(x => x.TryGetQuotaAsync(asset, productId, type, CancellationToken.None))
                .ReturnsAsync(quota)
                .Verifiable();
            Mock.Get(savings)
                .Setup(x => x.RedeemAsync(asset, productId, amount, type, CancellationToken.None))
                .Verifiable();

            var executor = new RedeemSavingsExecutor(logger, savings);
            var context = AlgoContext.Empty;
            var command = new RedeemSavingsCommand(asset, amount);

            // act
            var result = await executor.ExecuteAsync(context, command, CancellationToken.None);

            // assert
            Mock.Get(savings).VerifyAll();
            Assert.True(result.Success);
            Assert.Equal(amount, result.Redeemed);
        }
    }
}