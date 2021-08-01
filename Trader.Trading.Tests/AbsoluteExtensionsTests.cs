using System.Linq;
using Trader.Trading.Indicators;
using Xunit;

namespace Trader.Trading.Tests
{
    public static class AbsoluteExtensionsTests
    {
        [Fact]
        public static void EmitsEmptyOutput()
        {
            // arrange
            var input = Enumerable.Empty<decimal>();

            // act
            var output = input.Absolute();

            // assert
            Assert.Empty(output);
        }

        [Fact]
        public static void EmitsAbsoluteOutput()
        {
            // arrange
            var input = new decimal[] { 1, -1, 2, -3, 5, -8, 13, -21, 34, -55, 89, -144 };

            // act
            var output = input.Absolute();

            // assert
            Assert.Collection(output,
                x => Assert.Equal(1, x),
                x => Assert.Equal(1, x),
                x => Assert.Equal(2, x),
                x => Assert.Equal(3, x),
                x => Assert.Equal(5, x),
                x => Assert.Equal(8, x),
                x => Assert.Equal(13, x),
                x => Assert.Equal(21, x),
                x => Assert.Equal(34, x),
                x => Assert.Equal(55, x),
                x => Assert.Equal(89, x),
                x => Assert.Equal(144, x));
        }
    }
}