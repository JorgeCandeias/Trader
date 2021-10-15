using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Trading.Algorithms;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class AlgoFactoryResolverServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddsServices()
        {
            // arrange
            var services = new ServiceCollection();

            // act
            services.AddAlgoFactoryResolver();

            // assert
            var provider = services.BuildServiceProvider();
            var resolver = provider.GetService<IAlgoFactoryResolver>();
            Assert.IsType<AlgoFactoryResolver>(resolver);
        }
    }
}