using Outcompute.Trader.Trading.Indicators;
using System;
using System.Linq;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class SmaIteratorTests
    {
        [Fact]
        public void ThrowsOnNullSource()
        {
            // act
            static SmaIterator Test() => new(null!, 14);

            // assert
            Assert.Throws<ArgumentNullException>("source", Test);
        }

        [Fact]
        public void ThrowsOnLowPeriods()
        {
            // act
            static SmaIterator Test() => new(Enumerable.Empty<decimal>(), 0);

            // assert
            Assert.Throws<ArgumentOutOfRangeException>("periods", Test);
        }

        [Fact]
        public void EnumeratesEmpty()
        {
            // arrange
            var source = Enumerable.Empty<decimal>();
            var periods = 14;
            using var rma = new SmaIterator(source, periods);

            // act
            var result = rma.ToList();

            // assert
            Assert.Empty(result);
        }

        [Fact]
        public void CalculatesSma()
        {
            // arrange
            var periods = 14;
            var source = new decimal[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };

            // calculate smas one by one
            var expected = new decimal[20];
            expected[0] = source[0];
            for (var i = 1; i < source.Length; i++)
            {
                var sum = 0m;
                var count = 0;
                for (var j = i; j >= 0 && j > i - periods; j--)
                {
                    sum += source[j];
                    count++;
                }

                expected[i] = sum / count;
            }

            using var sma = new SmaIterator(source, periods);

            // act
            var result = sma.ToList();

            // assert
            Assert.Equal(source.Length, result.Count);
            for (var i = 0; i < source.Length; i++)
            {
                Assert.Equal(expected[i], result[i]);
            }
        }
    }
}