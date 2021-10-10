using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class SimpleMovingAverageExtensionsTests
    {
        [Fact]
        public void YieldsEmptyOutput()
        {
            // arrange
            var input = Enumerable.Empty<decimal>();

            // act
            var output = input.SimpleMovingAverages(3);

            // assert
            Assert.Empty(output);
        }

        [Fact]
        public void YieldsSimpleMovingAverage()
        {
            // arrange
            var input = new decimal[] { 1, 1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 144 };

            // act
            var output = input.SimpleMovingAverages(3);

            // assert
            Assert.Collection(output,
                x => Assert.Equal(0m, x),
                x => Assert.Equal(0m, x),
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