using System.Linq;
using Outcompute.Trader.Trading.Indicators;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class MovingSumExtensionsTests
    {
        [Fact]
        public void EmitsEmptyOutput()
        {
            // arrange
            var input = Enumerable.Empty<decimal>();

            // act
            var output = input.MovingSum(3);

            // assert
            Assert.Empty(output);
        }

        [Fact]
        public void EmitsMovingSum()
        {
            // arrange
            var input = new decimal[] { 1, 1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 144 };

            // act
            var output = input.MovingSum(3);

            // assert
            Assert.Collection(output,
                x => Assert.Equal(1, x),
                x => Assert.Equal(2, x),
                x => Assert.Equal(4, x),
                x => Assert.Equal(6, x),
                x => Assert.Equal(10, x),
                x => Assert.Equal(16, x),
                x => Assert.Equal(26, x),
                x => Assert.Equal(42, x),
                x => Assert.Equal(68, x),
                x => Assert.Equal(110, x),
                x => Assert.Equal(178, x),
                x => Assert.Equal(288, x));
        }
    }
}