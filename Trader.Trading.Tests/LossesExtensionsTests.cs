using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class LossesExtensionsTests
    {
        [Fact]
        public void YieldsEmptyResultOnEmptyInput()
        {
            // arrange
            var input = Enumerable.Empty<decimal>();

            // act
            var output = input.Losses();

            // assert
            Assert.Empty(output);
        }

        [Fact]
        public void YieldsPositiveChanges()
        {
            // arrange
            var input = new decimal[] { 1, 1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 144 };

            // act
            var output = input.Losses();

            // assert
            Assert.Collection(output,
                x => Assert.Equal(0, x),
                x => Assert.Equal(0, x),
                x => Assert.Equal(0, x),
                x => Assert.Equal(0, x),
                x => Assert.Equal(0, x),
                x => Assert.Equal(0, x),
                x => Assert.Equal(0, x),
                x => Assert.Equal(0, x),
                x => Assert.Equal(0, x),
                x => Assert.Equal(0, x),
                x => Assert.Equal(0, x),
                x => Assert.Equal(0, x));
        }

        [Fact]
        public void YieldsNegativeChanges()
        {
            // arrange
            var input = new decimal[] { 144, 89, 55, 34, 21, 13, 8, 5, 3, 2, 1, 1 };

            // act
            var output = input.Losses();

            // assert
            Assert.Collection(output,
                x => Assert.Equal(0, x),
                x => Assert.Equal(-55, x),
                x => Assert.Equal(-34, x),
                x => Assert.Equal(-21, x),
                x => Assert.Equal(-13, x),
                x => Assert.Equal(-8, x),
                x => Assert.Equal(-5, x),
                x => Assert.Equal(-3, x),
                x => Assert.Equal(-2, x),
                x => Assert.Equal(-1, x),
                x => Assert.Equal(-1, x),
                x => Assert.Equal(0, x));
        }

        [Fact]
        public void YieldsMixedChanges()
        {
            // arrange
            var input = new decimal[] { 1, 2, 1, 5, 3, 13, 8, 34, 21, 89, 55, 144 };

            // act
            var output = input.Losses();

            // assert
            Assert.Collection(output,
                x => Assert.Equal(0, x),
                x => Assert.Equal(0, x),
                x => Assert.Equal(-1, x),
                x => Assert.Equal(0, x),
                x => Assert.Equal(-2, x),
                x => Assert.Equal(0, x),
                x => Assert.Equal(-5, x),
                x => Assert.Equal(0, x),
                x => Assert.Equal(-13, x),
                x => Assert.Equal(0, x),
                x => Assert.Equal(-34, x),
                x => Assert.Equal(0, x));
        }
    }
}