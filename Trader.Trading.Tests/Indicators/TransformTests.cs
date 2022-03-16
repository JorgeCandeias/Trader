using Outcompute.Trader.Trading.Indicators;

namespace Outcompute.Trader.Trading.Tests.Indicators
{
    public class TransformTests
    {
        [Fact]
        public void YieldsTransform()
        {
            // act
            using var indicator = new Transform<decimal?, decimal?>(x => x * 2) { null, null, 2, 3, 5 };

            // assert
            Assert.Collection(indicator,
                x => Assert.Null(x),
                x => Assert.Null(x),
                x => Assert.Equal(4, x),
                x => Assert.Equal(6, x),
                x => Assert.Equal(10, x));
        }

        [Fact]
        public void UpdatesInPlace()
        {
            // arrange
            using var indicator = new Transform<decimal?, decimal?>(x => x * 2) { null, null, 2, 3, 5 };

            // act
            indicator.Update(1, 1);

            // assert
            Assert.Collection(indicator,
                x => Assert.Null(x),
                x => Assert.Equal(2, x),
                x => Assert.Equal(4, x),
                x => Assert.Equal(6, x),
                x => Assert.Equal(10, x));
        }

        [Fact]
        public void UpdatesFromSource()
        {
            // arrange
            using var source = new Identity<decimal?> { null, null, 2, 3, 5 };
            using var indicator = new Transform<decimal?, decimal?>(source, x => x * 2);

            // act
            source.Update(1, 1);

            // assert
            Assert.Collection(indicator,
                x => Assert.Null(x),
                x => Assert.Equal(2, x),
                x => Assert.Equal(4, x),
                x => Assert.Equal(6, x),
                x => Assert.Equal(10, x));
        }
    }
}