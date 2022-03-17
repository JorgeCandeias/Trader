using Outcompute.Trader.Trading.Indicators;
using Outcompute.Trader.Trading.Indicators.Operators;

namespace Outcompute.Trader.Trading.Tests.Indicators.Operators
{
    public static class AddTests
    {
        [Fact]
        public static void YieldsOutput()
        {
            // arrange
            using var first = new Identity<decimal?> { 1, 2, 3 };
            using var second = new Identity<decimal?> { 2, 3, 4 };

            // act
            using var indicator = new Add(first, second);

            // assert
            Assert.Collection(indicator,
                x => Assert.Equal(3, x),
                x => Assert.Equal(5, x),
                x => Assert.Equal(7, x));
        }

        [Fact]
        public static void ThrowsOnInPlaceUpdate()
        {
            // arrange
            using var first = new Identity<decimal?> { 1, 2, 3 };
            using var second = new Identity<decimal?> { 2, 3, 4 };
            using var indicator = new Add(first, second);

            // assert
            Assert.Throws<NotSupportedException>(() => indicator.Update(2, -4));
        }

        [Fact]
        public static void UpdatesFromSource()
        {
            // arrange
            using var first = new Identity<decimal?> { 1, 2, 3 };
            using var second = new Identity<decimal?> { 2, 3, 4 };
            using var indicator = new Add(first, second);

            // act
            first.Update(1, 5);
            second.Update(2, 9);

            // assert
            Assert.Collection(indicator,
                x => Assert.Equal(3, x),
                x => Assert.Equal(8, x),
                x => Assert.Equal(12, x));
        }
    }
}