using Outcompute.Trader.Indicators;

namespace Outcompute.Trader.Trading.Tests.Indicators
{
    public class SmaTests
    {
        private static decimal?[] CalculateSma(decimal?[] source, int periods)
        {
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
            return expected;
        }

        [Fact]
        public void YieldsSma()
        {
            // arrange
            var periods = 3;
            var source = new decimal?[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };

            // calculate smas one by one
            var expected = CalculateSma(source, periods);

            // act
            using var indicator = source.Identity().Sma(periods);

            // assert
            Assert.Equal(source.Length, indicator.Count);
            for (var i = 0; i < periods - 1; i++)
            {
                Assert.Null(indicator[i]);
            }
            for (var i = periods - 1; i < source.Length; i++)
            {
                Assert.Equal(expected[i], indicator[i]);
            }
        }

        [Fact]
        public void UpdatesFromSource()
        {
            // arrange
            var periods = 3;
            var source = new decimal?[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };

            using var identity = new Identity<decimal?>();
            identity.AddRange(source);

            using var indicator = new Sma(identity, periods);

            source[10] = 100;
            var expected = CalculateSma(source, periods);

            // act
            identity.Update(10, 100);

            // assert
            Assert.Equal(source.Length, indicator.Count);
            for (var i = 0; i < periods - 1; i++)
            {
                Assert.Null(indicator[i]);
            }
            for (var i = periods - 1; i < source.Length; i++)
            {
                Assert.Equal(expected[i], indicator[i]);
            }
        }
    }
}