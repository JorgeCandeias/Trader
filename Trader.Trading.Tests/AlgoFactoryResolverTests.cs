using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Commands;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class AlgoFactoryResolverTests
    {
        private class MyAlgo : Algo
        {
            protected override ValueTask<IAlgoCommand> OnExecuteAsync(CancellationToken cancellationToken = default)
            {
                return ValueTask.FromResult<IAlgoCommand>(Noop());
            }
        }

        [Fact]
        public void Resolves()
        {
            // arrange
            var provider = new ServiceCollection()
                .TryAddKeyedServiceCollection()
                .AddTradingServices()
                .AddAlgoType<MyAlgo>("MyAlgo")
                .BuildServiceProvider();

            var resolver = new AlgoFactoryResolver(provider);

            // act
            var factory = resolver.Resolve("MyAlgo");

            // assert
            var algo = factory.Create("Algo1");
            Assert.IsType<MyAlgo>(algo);
        }
    }
}