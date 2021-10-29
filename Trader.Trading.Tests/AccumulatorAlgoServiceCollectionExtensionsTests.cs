using Microsoft.Extensions.DependencyInjection;
using Orleans.Runtime;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Algorithms.Standard.Accumulator;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class AccumulatorAlgoServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddsServices()
        {
            // arrange
            var services = new ServiceCollection();

            // act
            services.AddAccumulatorAlgo();

            // assert
            var provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredServiceByName<IAlgoFactory>("Accumulator");
            Assert.IsType<AlgoFactory<AccumulatorAlgo>>(factory);
        }
    }
}