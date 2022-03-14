namespace Outcompute.Trader.Trading.Tests
{
    public class RmaIteratorTests
    {
        [Fact]
        public void ThrowsOnNullSource()
        {
            // act
            static void Test() => ((decimal?[])null!).RunningMovingAverage(14);

            // assert
            Assert.Throws<ArgumentNullException>("source", Test);
        }

        [Fact]
        public void ThrowsOnLowPeriods()
        {
            // act
            static void Test() => Enumerable.Empty<decimal?>().RunningMovingAverage(0);

            // assert
            Assert.Throws<ArgumentOutOfRangeException>("periods", Test);
        }

        [Fact]
        public void EnumeratesEmpty()
        {
            // arrange
            var source = Enumerable.Empty<decimal?>();

            // act
            var result = source.RunningMovingAverage(14).ToList();

            // assert
            Assert.Empty(result);
        }

        [Fact]
        public void CalculatesRma()
        {
            // arrange
            var periods = 14;
            var source = new decimal?[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };

            // calculate rmas one by one
            var expected = new decimal?[20];
            expected[0] = source[0];
            for (var i = 1; i < source.Length; i++)
            {
                expected[i] = ((expected[i - 1] * (periods - 1)) + source[i]) / periods;
            }

            // act
            var result = source.RunningMovingAverage(periods).ToList();

            // assert
            Assert.Equal(source.Length, result.Count);
            for (var i = 0; i < source.Length; i++)
            {
                Assert.Equal(expected[i], result[i]);
            }
        }
    }
}