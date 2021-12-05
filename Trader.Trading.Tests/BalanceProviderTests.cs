using AutoMapper;
using Orleans;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Hosting;
using Outcompute.Trader.Trading.Providers.Balances;
using System.Collections.Immutable;

namespace Outcompute.Trader.Trading.Tests
{
    public class BalanceProviderTests
    {
        private readonly IMapper _mapper = new MapperConfiguration(options => options.AddProfile<ModelsProfile>()).CreateMapper();

        [Fact]
        public async Task SetsBalances()
        {
            // arrange
            var asset1 = Guid.NewGuid().ToString();
            var asset2 = Guid.NewGuid().ToString();

            var balance1 = AccountBalance.Empty with { Asset = asset1, Free = 123m };
            var balance2 = AccountBalance.Empty with { Asset = asset2, Free = 234m };

            var factory = Mock.Of<IGrainFactory>();
            Mock.Get(factory)
                .Setup(x => x.GetGrain<IBalanceProviderGrain>(asset1, null).SetBalanceAsync(It.Is<Balance>(x => x.Asset == balance1.Asset && x.Free == balance1.Free)))
                .Verifiable();
            Mock.Get(factory)
                .Setup(x => x.GetGrain<IBalanceProviderGrain>(asset2, null).SetBalanceAsync(It.Is<Balance>(x => x.Asset == balance2.Asset && x.Free == balance2.Free)))
                .Verifiable();

            var repository = Mock.Of<ITradingRepository>();
            var provider = new BalanceProvider(factory, repository, _mapper);

            var info = AccountInfo.Empty with { AccountType = AccountType.Spot, Balances = ImmutableList.Create(balance1, balance2) };

            // act
            await provider.SetBalancesAsync(info);

            // assert
            Mock.Get(factory).VerifyAll();
        }

        [Fact]
        public async Task GetsBalances()
        {
            // arrange
            var balances = new[]
            {
                Balance.Empty with { Asset = "ABC", Free = 123 },
                Balance.Empty with { Asset = "XYZ", Free = 234 }
            };

            var repository = Mock.Of<ITradingRepository>();
            Mock.Get(repository)
                .Setup(x => x.GetBalancesAsync(CancellationToken.None))
                .ReturnsAsync(balances)
                .Verifiable();

            var factory = Mock.Of<IGrainFactory>();
            var provider = new BalanceProvider(factory, repository, _mapper);

            // act
            var result = await provider.GetBalancesAsync();

            // assert
            Assert.Same(balances, result);
            Mock.Get(factory).VerifyAll();
        }

        [Fact]
        public async Task GetsBalance()
        {
            // arrange
            var balance = Balance.Empty with { Asset = "ABC", Free = 123 };

            var repository = Mock.Of<ITradingRepository>();

            var factory = Mock.Of<IGrainFactory>();
            Mock.Get(factory)
                .Setup(x => x.GetGrain<IBalanceProviderReplicaGrain>(balance.Asset, null).TryGetBalanceAsync())
                .ReturnsAsync(balance)
                .Verifiable();

            var provider = new BalanceProvider(factory, repository, _mapper);

            // act
            var result = await provider.TryGetBalanceAsync(balance.Asset);

            // assert
            Assert.Same(balance, result);
            Mock.Get(factory).VerifyAll();
        }
    }
}