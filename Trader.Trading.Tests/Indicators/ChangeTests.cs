using Outcompute.Trader.Trading.Indicators;

namespace Outcompute.Trader.Trading.Tests.Indicators
{
    public class ChangeTests
    {
        [Fact]
        public void YieldsPositiveChanges()
        {
            // act
            using var indicator = Indicator
                .Identity<decimal?>(1, 1, 2, 3, 5)
                .Change(2);

            // assert
            Assert.Collection(indicator,
                x => Assert.Null(x),
                x => Assert.Null(x),
                x => Assert.Equal(1, x),
                x => Assert.Equal(2, x),
                x => Assert.Equal(3, x));
        }

        [Fact]
        public void YieldsNegativeChanges()
        {
            // act
            using var indicator = Indicator
                .Identity<decimal?>(144, 89, 55, 34, 21)
                .Change(2);

            // assert
            Assert.Collection(indicator,
                x => Assert.Null(x),
                x => Assert.Null(x),
                x => Assert.Equal(-89, x),
                x => Assert.Equal(-55, x),
                x => Assert.Equal(-34, x));
        }

        [Fact]
        public void YieldsMixedChanges()
        {
            // act
            using var indicator = Indicator
                .Identity<decimal?>(1, 9, 1, 5, 3)
                .Change(2);

            // assert
            Assert.Collection(indicator,
                x => Assert.Null(x),
                x => Assert.Null(x),
                x => Assert.Equal(0, x),
                x => Assert.Equal(-4, x),
                x => Assert.Equal(2, x));
        }

        [Fact]
        public void UpdatesFromSource()
        {
            // arrange
            using var source = new Identity<decimal?> { 1, 9, 1, 5, 3 };
            using var indicator = new Change(source, 2);

            // act
            source.Update(2, 2);

            // assert
            Assert.Collection(indicator,
                x => Assert.Null(x),
                x => Assert.Null(x),
                x => Assert.Equal(1, x),
                x => Assert.Equal(-4, x),
                x => Assert.Equal(1, x));
        }
    }
}