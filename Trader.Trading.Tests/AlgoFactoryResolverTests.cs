using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Trading.Algorithms;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class AlgoFactoryResolverTests
    {
        private class MyAlgo : IAlgo
        {
            public ValueTask GoAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        }

        [Fact]
        public void Resolves()
        {
            // arrange
            var services = new ServiceCollection();
            services.AddAlgoType<MyAlgo>("MyAlgo");
            var provider = services.BuildServiceProvider();
            var resolver = new AlgoFactoryResolver(provider);

            // act
            var factory = resolver.Resolve("MyAlgo");

            // assert
            var algo = factory.Create("Algo1");
            Assert.IsType<MyAlgo>(algo);
        }
    }
}