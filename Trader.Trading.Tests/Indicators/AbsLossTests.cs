namespace Outcompute.Trader.Trading.Tests
{
    public class AbsLossTests
    {
        [Fact]
        public void YieldsEmptyResultOnEmptyInput()
        {
            // act
            using var indicator = new AbsLoss();

            // assert
            Assert.Empty(indicator);
        }

        [Fact]
        public void YieldsPositiveChanges()
        {
            // act
            using var indicator = new AbsLoss
            {
                1, 1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 144
            };

            // assert
            Assert.True(indicator.SequenceEqual(new decimal?[] { null, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }));
        }

        [Fact]
        public void YieldsNegativeChanges()
        {
            // act
            using var indicator = new AbsLoss
            {
                144, 89, 55, 34, 21, 13, 8, 5, 3, 2, 1, 1
            };

            // assert
            Assert.True(indicator.SequenceEqual(new decimal?[] { null, 55, 34, 21, 13, 8, 5, 3, 2, 1, 1, 0 }));
        }

        [Fact]
        public void YieldsMixedChanges()
        {
            // act
            using var indicator = new AbsLoss
            {
                1, 2, 1, 5, 3, 13, 8, 34, 21, 89, 55, 144
            };

            // assert
            Assert.True(indicator.SequenceEqual(new decimal?[] { null, 0, 1, 0, 2, 0, 5, 0, 13, 0, 34, 0 }));
        }
    }
}