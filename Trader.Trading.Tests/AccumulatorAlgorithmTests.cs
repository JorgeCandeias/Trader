using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Trader.Core.Time;
using Trader.Data;
using Trader.Trading.Algorithms;
using Trader.Trading.Algorithms.Accumulator;
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
            var orderSynchronizer = Mock.Of<IOrderSynchronizer>();
            var tradeSynchronizer = Mock.Of<ITradeSynchronizer>();
            var repository = Mock.Of<ITraderRepository>();

            // act
            var algo = new AccumulatorAlgorithm(name, options, logger, trader, clock, orderSynchronizer, tradeSynchronizer, repository);

            // assert
            Assert.Equal("SomeSymbol", algo.Symbol);
        }
    }
}