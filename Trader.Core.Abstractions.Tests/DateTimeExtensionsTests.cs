using System;
using Xunit;

namespace Outcompute.Trader.Core.Abstractions.Tests
{
    public class DateTimeExtensionsTests
    {
        [Fact]
        public void PreviousThrowsOnInvalidCount()
        {
            // arrange
            var date = DateTime.Today;

            // act
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                _ = date.Previous(DayOfWeek.Monday, 0);
            });

            // assert
            Assert.Equal("count", exception.ParamName);
        }

        [Fact]
        public void PreviousFindsEarlierDayOfWeek()
        {
            // arrange
            var date = new DateTime(2021, 4, 15); // thursday

            // act assert
            Assert.Equal(new DateTime(2021, 4, 9), date.Previous(DayOfWeek.Friday));
            Assert.Equal(new DateTime(2021, 4, 10), date.Previous(DayOfWeek.Saturday));
            Assert.Equal(new DateTime(2021, 4, 11), date.Previous(DayOfWeek.Sunday));
            Assert.Equal(new DateTime(2021, 4, 12), date.Previous(DayOfWeek.Monday));
            Assert.Equal(new DateTime(2021, 4, 13), date.Previous(DayOfWeek.Tuesday));
            Assert.Equal(new DateTime(2021, 4, 14), date.Previous(DayOfWeek.Wednesday));
            Assert.Equal(new DateTime(2021, 4, 15), date.Previous(DayOfWeek.Thursday));
        }

        [Fact]
        public void PreviousFindsEarlierDayOfPreviousWeek()
        {
            // arrange
            var date = new DateTime(2021, 4, 15); // thursday
            var count = 2;

            // act assert
            Assert.Equal(new DateTime(2021, 4, 2), date.Previous(DayOfWeek.Friday, count));
            Assert.Equal(new DateTime(2021, 4, 3), date.Previous(DayOfWeek.Saturday, count));
            Assert.Equal(new DateTime(2021, 4, 4), date.Previous(DayOfWeek.Sunday, count));
            Assert.Equal(new DateTime(2021, 4, 5), date.Previous(DayOfWeek.Monday, count));
            Assert.Equal(new DateTime(2021, 4, 6), date.Previous(DayOfWeek.Tuesday, count));
            Assert.Equal(new DateTime(2021, 4, 7), date.Previous(DayOfWeek.Wednesday, count));
            Assert.Equal(new DateTime(2021, 4, 8), date.Previous(DayOfWeek.Thursday, count));
        }
    }
}