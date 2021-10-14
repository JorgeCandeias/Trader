using Moq;
using Outcompute.Trader.Trading.Algorithms;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class AlgoFactoryTests
    {
        private class MyAlgo : IAlgo
        {
            public ValueTask GoAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        }

        [Fact]
        public void CreatesAlgo()
        {
            // arrange
            var provider = Mock.Of<IServiceProvider>();
            var factory = new AlgoFactory<MyAlgo>(provider);

            // act
            var algo = factory.Create("Algo1");

            // assert
            Assert.IsType<MyAlgo>(algo);
        }
    }
}