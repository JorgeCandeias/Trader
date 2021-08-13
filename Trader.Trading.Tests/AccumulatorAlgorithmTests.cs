using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Trader.Trading.Algorithms.Accumulator;
using Trader.Trading.Algorithms.Steps;
using Xunit;

namespace Trader.Trading.Tests
{
    public class AccumulatorAlgorithmTests
    {
        [Fact]
        public void Constructs()
        {
            // arrange
            var name = "SomeSymbol";
            var options = Mock.Of<IOptionsSnapshot<AccumulatorAlgorithmOptions>>(x => x.Get(name) == new AccumulatorAlgorithmOptions
            {
                Symbol = "SomeSymbol"
            });
            var logger = NullLogger<AccumulatorAlgorithm>.Instance;
            var trackingBuyStep = Mock.Of<ITrackingBuyStep>();

            // act
            var algo = new AccumulatorAlgorithm(name, options, logger, trackingBuyStep);

            // assert
            Assert.Equal("SomeSymbol", algo.Symbol);
        }
    }
}