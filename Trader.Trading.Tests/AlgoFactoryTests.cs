using Microsoft.Extensions.DependencyInjection;
using Moq;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Algorithms.Context;
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
            protected override ValueTask<IAlgoCommand> OnExecuteAsync(CancellationToken cancellationToken = default)
            {
                return ValueTask.FromResult<IAlgoCommand>(Noop());
            }
        }

        [Fact]
        public void CreatesAlgo()
        {
            // arrange
            var name = "Algo1";

            var context = Mock.Of<IAlgoContext>();

            var contexts = Mock.Of<IAlgoContextFactory>(
                x => x.Create(name) == context);

            var provider = new ServiceCollection()
                .AddTradingServices()
                .BuildServiceProvider();

            var factory = new AlgoFactory<MyAlgo>(contexts, provider);

            // act
            var algo = factory.Create("Algo1");

            // assert
            Assert.IsType<MyAlgo>(algo);
        }
    }
}