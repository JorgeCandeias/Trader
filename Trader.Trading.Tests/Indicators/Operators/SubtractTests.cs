using Outcompute.Trader.Trading.Indicators;
using Outcompute.Trader.Trading.Indicators.Operators;

namespace Outcompute.Trader.Trading.Tests.Indicators.Operators
{
    public static class SubtractTests
    {
        [Fact]
        public static void YieldsOutput()
        {
            // arrange
            using var first = new Identity<decimal?> { 2, 3, 4, 1, null };
            using var second = new Identity<decimal?> { 4, 3, 2, null, 1 };

            // act
            using var indicator = new Subtract(first, second);

            // assert
            Assert.Collection(indicator,
                x => Assert.Equal(-2, x),
                x => Assert.Equal(0, x),
                x => Assert.Equal(2, x),
                x => Assert.Null(x),
                x => Assert.Null(x));
        }

        [Fact]
        public static void UpdatesFromSource()
        {
            // arrange
            using var first = new Identity<decimal?> { 2, 3, 4, 1, null };
            using var second = new Identity<decimal?> { 4, 3, 2, null, 1 };
            using var indicator = new Subtract(first, second);

            // act
            first.Update(1, 5);
            second.Update(2, 9);

            // assert
            Assert.Collection(indicator,
                x => Assert.Equal(-2, x),
                x => Assert.Equal(2, x),
                x => Assert.Equal(-5, x),
                x => Assert.Null(x),
                x => Assert.Null(x));
        }
    }
}