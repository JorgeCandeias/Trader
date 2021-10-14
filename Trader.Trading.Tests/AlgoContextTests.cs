using Moq;
using Outcompute.Trader.Trading.Algorithms;
using System;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class AlgoContextTests
    {
        [Fact]
        public void Constructs()
        {
            // arrange
            AlgoFactoryContext.AlgoName = "MyAlgoName";
            var provider = Mock.Of<IServiceProvider>();

            // act
            var context = new AlgoContext(provider);

            // asset
            Assert.Same(AlgoFactoryContext.AlgoName, context.Name);
            Assert.Same(provider, context.ServiceProvider);
        }
    }
}