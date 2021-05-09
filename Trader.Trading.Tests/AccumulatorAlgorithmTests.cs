using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Trader.Core.Time;
using Trader.Data;
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
            var trader = Mock.Of<ITradingService>();
            var clock = Mock.Of<ISystemClock>();
            var repository = Mock.Of<ITraderRepository>();
            var trackingBuyStep = Mock.Of<ITrackingBuyStep>();

            // act
            var algo = new AccumulatorAlgorithm(name, options, logger, trader, clock, repository, trackingBuyStep);

            // assert
            Assert.Equal("SomeSymbol", algo.Symbol);
        }
    }
}