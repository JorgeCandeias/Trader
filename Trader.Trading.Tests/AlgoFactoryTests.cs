using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Trading.Algorithms;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class AlgoFactoryTests
    {
        private class MyAlgo : Algo
        {
            public override Task GoAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        }

        [Fact]
        public void CreatesAlgo()
        {
            // arrange
            var provider = new ServiceCollection()
                .AddTradingServices()
                .BuildServiceProvider();

            var factory = new AlgoFactory<MyAlgo>(provider);

            // act
            var algo = factory.Create("Algo1");

            // assert
            Assert.IsType<MyAlgo>(algo);
        }
    }
}