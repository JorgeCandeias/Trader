using Microsoft.Extensions.DependencyInjection;
using Moq;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Providers;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class ExchangeInfoProviderAlgoContextExtensionsTests
    {
        [Fact]
        public void GetExchangeInfoProvider()
        {
            // arrange
            var exchange = Mock.Of<IExchangeInfoProvider>();
            var provider = new ServiceCollection()
                .AddSingleton(exchange)
                .BuildServiceProvider();
            var context = new AlgoContext("Algo1", provider);

            // act
            var result = context.GetExchangeInfoProvider();

            // assert
            Assert.Same(exchange, result);
        }
    }
}