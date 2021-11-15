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
            var name = "Algo1";
            var algoContextFactory = Mock.Of<IAlgoContextFactory>(x => x.Create(name) == AlgoContext.Empty);

            var provider = new ServiceCollection()
                .TryAddKeyedServiceCollection()
                .AddAlgoType<MyAlgo>("MyAlgo").Services
                .AddSingleton(algoContextFactory)
                .BuildServiceProvider();

            var resolver = new AlgoFactoryResolver(provider);

            // act
            var factory = resolver.Resolve("MyAlgo");

            // assert
            var algo = factory.Create(name);
            Assert.IsType<MyAlgo>(algo);
        }
    }
}