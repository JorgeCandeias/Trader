using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Commands;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class AlgoFactoryTests
    {
        private class MyAlgo : Algo
        {
            public override Task<IAlgoCommand> GoAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult<IAlgoCommand>(Noop());
            }
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