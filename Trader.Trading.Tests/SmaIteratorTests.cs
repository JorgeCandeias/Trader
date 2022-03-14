namespace Outcompute.Trader.Trading.Tests
{
    public class SmaIteratorTests
    {
        [Fact]
        public void EnumeratesEmpty()
        {
            // arrange
            var source = Enumerable.Empty<decimal?>();
            var periods = 14;

            // act
            var result = source.SimpleMovingAverage(periods).ToList();

            // assert
            Assert.Empty(result);
        }

        [Fact]
        public void CalculatesSma()
        {
            // arrange
            var periods = 14;
            var source = new decimal?[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };

            // calculate smas one by one
            var expected = new decimal?[20];
            expected[0] = source[0];
            for (var i = 1; i < source.Length; i++)
            {
                var sum = 0m;
                var count = 0;
                for (var j = i; j >= 0 && j > i - periods; j--)
                {
                    sum += source[j].GetValueOrDefault(0);
                    count++;
                }

                expected[i] = sum / count;
            }

            // act
            var result = source.SimpleMovingAverage(periods).ToList();

            // assert
            Assert.Equal(source.Length, result.Count);
            for (var i = 0; i < source.Length; i++)
            {
                Assert.Equal(expected[i], result[i]);
            }
        }
    }
}