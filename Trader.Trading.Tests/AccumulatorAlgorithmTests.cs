using Microsoft.Extensions.Options;
using Moq;
using Outcompute.Trader.Trading.Algorithms.Standard.Accumulator;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class AccumulatorAlgorithmTests
    {
        [Fact]
        public void Constructs()
        {
            // arrange
            var options = Mock.Of<IOptionsSnapshot<AccumulatorAlgoOptions>>(x => x.Value.Symbol == "SomeSymbol");

            // act
            var algo = new AccumulatorAlgo(options);

            // assert
            Assert.NotNull(algo);
        }
    }
}