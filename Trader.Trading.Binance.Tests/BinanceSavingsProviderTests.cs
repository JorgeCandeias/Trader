using Moq;
using Orleans;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Binance.Providers.Savings;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.Binance.Tests
{
    public class BinanceSavingsProviderTests
    {
        [Fact]
        public async Task GetFlexibleProductPositionsAsync()
        {
            // arrange
            var asset = "ABCXYZ";
            var positions = new[] { FlexibleProductPosition.Zero(asset) with { ProductId = "P1" } };
            var grain = Mock.Of<IBinanceSavingsGrain>(x => x.GetFlexibleProductPositionsAsync() == ValueTask.FromResult<IReadOnlyCollection<FlexibleProductPosition>>(positions));
            var factory = Mock.Of<IGrainFactory>(x => x.GetGrain<IBinanceSavingsGrain>(asset, null) == grain);
            var provider = new BinanceSavingsProvider(factory);

            // act
            var result = await provider.GetFlexibleProductPositionAsync(asset, CancellationToken.None);

            // assert
            Assert.Equal(1, result.Count);
            Assert.Equal(positions.First(), result.First());
        }

        [Fact]
        public async Task TryGetFirstFlexibleProductPositionAsync()
        {
            // arrange
            var asset = "ABCXYZ";
            var position = FlexibleProductPosition.Zero(asset) with { ProductId = "P1" };
            var grain = Mock.Of<IBinanceSavingsGrain>(x => x.TryGetFirstFlexibleProductPositionAsync() == ValueTask.FromResult<FlexibleProductPosition?>(position));
            var factory = Mock.Of<IGrainFactory>(x => x.GetGrain<IBinanceSavingsGrain>(asset, null) == grain);
            var provider = new BinanceSavingsProvider(factory);

            // act
            var result = await provider.TryGetFirstFlexibleProductPositionAsync(asset, CancellationToken.None);

            // assert
            Assert.Equal(position, result);
        }

        [Fact]
        public async Task TryGetLeftDailyRedemptionQuotaOnFlexibleProductAsync()
        {
            // arrange
            var asset = "ABCXYZ";
            var productId = "P1";
            var type = FlexibleProductRedemptionType.Fast;
            var quota = LeftDailyRedemptionQuotaOnFlexibleProduct.Empty with { Asset = "ABCXYZ", LeftQuota = 1000000m };
            var grain = Mock.Of<IBinanceSavingsGrain>(x => x.TryGetLeftDailyRedemptionQuotaOnFlexibleProductAsync(productId, type) == ValueTask.FromResult<LeftDailyRedemptionQuotaOnFlexibleProduct?>(quota));
            var factory = Mock.Of<IGrainFactory>(x => x.GetGrain<IBinanceSavingsGrain>(asset, null) == grain);
            var provider = new BinanceSavingsProvider(factory);

            // act
            var result = await provider.TryGetLeftDailyRedemptionQuotaOnFlexibleProductAsync(asset, productId, type, CancellationToken.None);

            // assert
            Assert.Equal(quota, result);
        }

        [Fact]
        public async Task RedeemFlexibleProductAsync()
        {
            // arrange
            var asset = "ABCXYZ";
            var productId = "P1";
            var amount = 123m;
            var type = FlexibleProductRedemptionType.Fast;
            var grain = Mock.Of<IBinanceSavingsGrain>();
            Mock.Get(grain).Setup(x => x.RedeemFlexibleProductAsync(productId, amount, type)).Verifiable();
            var factory = Mock.Of<IGrainFactory>(x => x.GetGrain<IBinanceSavingsGrain>(asset, null) == grain);
            var provider = new BinanceSavingsProvider(factory);

            // act
            await provider.RedeemFlexibleProductAsync(asset, productId, amount, type, CancellationToken.None);

            // assert
            Mock.Get(grain).Verify();
        }
    }
}