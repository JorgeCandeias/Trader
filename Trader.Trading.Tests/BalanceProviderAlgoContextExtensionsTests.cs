using Microsoft.Extensions.DependencyInjection;
using Moq;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Providers;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class BalanceProviderAlgoContextExtensionsTests
    {
        [Fact]
        public void GetBalanceProvider()
        {
            // arrange
            var balances = Mock.Of<IBalanceProvider>();

            var provider = new ServiceCollection()
                .AddSingleton(balances)
                .BuildServiceProvider();

            var context = Mock.Of<IAlgoContext>(x =>
                x.ServiceProvider == provider);

            // act
            var result = context.GetBalanceProvider();

            // assert
            Assert.Same(balances, result);
        }
    }
}