using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Trading.Algorithms;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class AlgoFactoryResolverTests
    {
        private class MyAlgo : Algo
        {
            public override Task<IAlgoCommand> GoAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult<IAlgoCommand>(Noop());
            }
        }

        [Fact]
        public void Resolves()
        {
            // arrange
            var provider = new ServiceCollection()
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