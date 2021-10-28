using Microsoft.Extensions.Options;
using Moq;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Algorithms.Accumulator;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class AccumulatorAlgorithmTests
    {
        [Fact]
        public void Constructs()
        {
            // arrange
            var context = Mock.Of<IAlgoContext>();
            var name = "SomeSymbol";
            var options = Mock.Of<IOptionsMonitor<AccumulatorAlgoOptions>>(x => x.Get(name) == new AccumulatorAlgoOptions
            {
                Symbol = "SomeSymbol"
            });

            // act
            var algo = new AccumulatorAlgo(context, options);

            // assert
            Assert.NotNull(algo);
        }
    }
}