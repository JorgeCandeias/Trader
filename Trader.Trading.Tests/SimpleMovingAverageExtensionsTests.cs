using System.Linq;
using Trader.Trading.Indicators;
using Xunit;

namespace Trader.Trading.Tests
{
    public class SimpleMovingAverageExtensionsTests
    {
        [Fact]
        public void EmitsEmptyOutput()
        {
            // arrange
            var input = Enumerable.Empty<decimal>();

            // act
            var output = input.SimpleMovingAverage(3);

            // assert
            Assert.Empty(output);
        }

        [Fact]
        public void EmitsSimpleMovingAverage()
        {
            // arrange
            var input = new decimal[] { 1, 1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 144 };

            // act
            var output = input.SimpleMovingAverage(3);

            // assert
            Assert.Collection(output,
                x => Assert.Equal(1m, x),
                x => Assert.Equal(1m, x),
                x => Assert.Equal(4m / 3m, x),
                x => Assert.Equal(6m / 3m, x),
                x => Assert.Equal(10m / 3m, x),
                x => Assert.Equal(16m / 3m, x),
                x => Assert.Equal(26m / 3m, x),
                x => Assert.Equal(42m / 3m, x),
                x => Assert.Equal(68m / 3m, x),
                x => Assert.Equal(110m / 3m, x),
                x => Assert.Equal(178m / 3m, x),
                x => Assert.Equal(288m / 3m, x));
        }
    }
}