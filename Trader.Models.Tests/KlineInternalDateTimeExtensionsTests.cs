using System;
using System.Collections.Generic;
using Xunit;

namespace Outcompute.Trader.Models.Tests
{
    public class KlineInternalDateTimeExtensionsTests
    {
        [Fact]
        public void RangeCreatesEmptyRange()
        {
            // arrange
            var start = DateTime.Today;
            var end = start.AddDays(-1);
            var interval = KlineInterval.Days1;

            // act
            var results = interval.Range(start, end);

            // assert
            Assert.Empty(results);
        }

        [Fact]
        public void RangeCreatesSingleRange()
        {
            // arrange
            var start = DateTime.Today;
            var end = DateTime.Today;
            var interval = KlineInterval.Days1;

            // act
            var results = interval.Range(start, end);

            // assert
            var result = Assert.Single(results);
            Assert.Equal(DateTime.Today, result);
        }

        [Fact]
        public void RangeCreatesFullRange()
        {
            // arrange
            var start = DateTime.Today;
            var end = DateTime.Today.AddDays(3);
            var interval = KlineInterval.Days1;

            // act
            var results = interval.Range(start, end);

            // assert
            Assert.Collection(results,
                x => Assert.Equal(DateTime.Today, x),
                x => Assert.Equal(DateTime.Today.AddDays(1), x),
                x => Assert.Equal(DateTime.Today.AddDays(2), x),
                x => Assert.Equal(DateTime.Today.AddDays(3), x));
        }

        [Fact]
        public void RangeDescendingCreatesEmptyRange()
        {
            // arrange
            var start = DateTime.Today;
            var end = start.AddDays(-1);
            var interval = KlineInterval.Days1;

            // act
            var results = interval.RangeDescending(start, end);

            // assert
            Assert.Empty(results);
        }

        [Fact]
        public void RangeDescendingCreatesSingleRange()
        {
            // arrange
            var start = DateTime.Today;
            var end = DateTime.Today;
            var interval = KlineInterval.Days1;

            // act
            var results = interval.RangeDescending(start, end);

            // assert
            var result = Assert.Single(results);
            Assert.Equal(DateTime.Today, result);
        }

        [Fact]
        public void RangeDescendingCreatesFullRange()
        {
            // arrange
            var start = DateTime.Today;
            var end = DateTime.Today.AddDays(3);
            var interval = KlineInterval.Days1;

            // act
            var results = interval.RangeDescending(start, end);

            // assert
            Assert.Collection(results,
                x => Assert.Equal(DateTime.Today.AddDays(3), x),
                x => Assert.Equal(DateTime.Today.AddDays(2), x),
                x => Assert.Equal(DateTime.Today.AddDays(1), x),
                x => Assert.Equal(DateTime.Today, x));
        }

        [Fact]
        public void AdjustToPreviousNoneThrows()
        {
            // arrange
            var value = DateTime.Today;

            // act
            void Test() => value.AdjustToPrevious(KlineInterval.None);

            // assert
            Assert.Throws<ArgumentOutOfRangeException>("interval", Test);
        }

        public static IEnumerable<object[]> AdjustToPreviousData { get; } = new[]
        {
            new object[] { KlineInterval.Minutes1, new DateTime(2021, 11, 1, 19, 11, 8, 242), new DateTime(2021, 11, 1, 19, 11, 0, 0) },
            new object[] { KlineInterval.Minutes3, new DateTime(2021, 11, 1, 19, 11, 8, 242), new DateTime(2021, 11, 1, 19, 9, 0, 0) },
            new object[] { KlineInterval.Minutes5, new DateTime(2021, 11, 1, 19, 11, 8, 242), new DateTime(2021, 11, 1, 19, 10, 0, 0) },
            new object[] { KlineInterval.Minutes15, new DateTime(2021, 11, 1, 19, 11, 8, 242), new DateTime(2021, 11, 1, 19, 0, 0, 0) },
            new object[] { KlineInterval.Minutes30, new DateTime(2021, 11, 1, 19, 11, 8, 242), new DateTime(2021, 11, 1, 19, 0, 0, 0) },
            new object[] { KlineInterval.Hours1, new DateTime(2021, 11, 1, 19, 11, 8, 242), new DateTime(2021, 11, 1, 19, 0, 0, 0) },
            new object[] { KlineInterval.Hours2, new DateTime(2021, 11, 1, 19, 11, 8, 242), new DateTime(2021, 11, 1, 18, 0, 0, 0) },
            new object[] { KlineInterval.Hours4, new DateTime(2021, 11, 1, 19, 11, 8, 242), new DateTime(2021, 11, 1, 16, 0, 0, 0) },
            new object[] { KlineInterval.Hours6, new DateTime(2021, 11, 1, 19, 11, 8, 242), new DateTime(2021, 11, 1, 18, 0, 0, 0) },
            new object[] { KlineInterval.Hours8, new DateTime(2021, 11, 1, 19, 11, 8, 242), new DateTime(2021, 11, 1, 16, 0, 0, 0) },
            new object[] { KlineInterval.Hours12, new DateTime(2021, 11, 1, 19, 11, 8, 242), new DateTime(2021, 11, 1, 12, 0, 0, 0) },
            new object[] { KlineInterval.Days1, new DateTime(2021, 11, 1, 19, 11, 8, 242), new DateTime(2021, 11, 1, 0, 0, 0, 0) },
            new object[] { KlineInterval.Days3, new DateTime(2021, 11, 5, 19, 11, 8, 242), new DateTime(2021, 11, 4, 0, 0, 0, 0) },
            new object[] { KlineInterval.Weeks1, new DateTime(2021, 11, 5, 19, 11, 8, 242), new DateTime(2021, 10, 31, 0, 0, 0, 0) },
            new object[] { KlineInterval.Months1, new DateTime(2021, 11, 5, 19, 11, 8, 242), new DateTime(2021, 11, 1, 0, 0, 0, 0) },
        };

        [Theory]
        [MemberData(nameof(AdjustToPreviousData))]
        public void AdjustToPrevious(KlineInterval interval, DateTime value, DateTime expected)
        {
            // act
            var result = value.AdjustToPrevious(interval);

            // assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void AdjustToNextNoneThrows()
        {
            // arrange
            var value = DateTime.Today;

            // act
            void Test() => value.AdjustToNext(KlineInterval.None);

            // assert
            Assert.Throws<ArgumentOutOfRangeException>("interval", Test);
        }

        public static IEnumerable<object[]> AdjustToNextData { get; } = new[]
{
            new object[] { KlineInterval.Minutes1, new DateTime(2021, 11, 1, 19, 11, 8, 242), new DateTime(2021, 11, 1, 19, 12, 0, 0) },
            new object[] { KlineInterval.Minutes3, new DateTime(2021, 11, 1, 19, 11, 8, 242), new DateTime(2021, 11, 1, 19, 12, 0, 0) },
            new object[] { KlineInterval.Minutes5, new DateTime(2021, 11, 1, 19, 11, 8, 242), new DateTime(2021, 11, 1, 19, 15, 0, 0) },
            new object[] { KlineInterval.Minutes15, new DateTime(2021, 11, 1, 19, 11, 8, 242), new DateTime(2021, 11, 1, 19, 15, 0, 0) },
            new object[] { KlineInterval.Minutes30, new DateTime(2021, 11, 1, 19, 11, 8, 242), new DateTime(2021, 11, 1, 19, 30, 0, 0) },
            new object[] { KlineInterval.Hours1, new DateTime(2021, 11, 1, 19, 11, 8, 242), new DateTime(2021, 11, 1, 20, 0, 0, 0) },
            new object[] { KlineInterval.Hours2, new DateTime(2021, 11, 1, 19, 11, 8, 242), new DateTime(2021, 11, 1, 20, 0, 0, 0) },
            new object[] { KlineInterval.Hours4, new DateTime(2021, 11, 1, 19, 11, 8, 242), new DateTime(2021, 11, 1, 20, 0, 0, 0) },
            new object[] { KlineInterval.Hours6, new DateTime(2021, 11, 1, 19, 11, 8, 242), new DateTime(2021, 11, 2, 0, 0, 0, 0) },
            new object[] { KlineInterval.Hours8, new DateTime(2021, 11, 1, 19, 11, 8, 242), new DateTime(2021, 11, 2, 0, 0, 0, 0) },
            new object[] { KlineInterval.Hours12, new DateTime(2021, 11, 1, 19, 11, 8, 242), new DateTime(2021, 11, 2, 0, 0, 0, 0) },
            new object[] { KlineInterval.Days1, new DateTime(2021, 11, 1, 19, 11, 8, 242), new DateTime(2021, 11, 2, 0, 0, 0, 0) },
            new object[] { KlineInterval.Days3, new DateTime(2021, 11, 5, 19, 11, 8, 242), new DateTime(2021, 11, 7, 0, 0, 0, 0) },
            new object[] { KlineInterval.Weeks1, new DateTime(2021, 11, 5, 19, 11, 8, 242), new DateTime(2021, 11, 7, 0, 0, 0, 0) },
            new object[] { KlineInterval.Months1, new DateTime(2021, 11, 5, 19, 11, 8, 242), new DateTime(2021, 12, 1, 0, 0, 0, 0) },
        };

        [Theory]
        [MemberData(nameof(AdjustToNextData))]
        public void AdjustToNext(KlineInterval interval, DateTime value, DateTime expected)
        {
            // act
            var result = value.AdjustToNext(interval);

            // assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void AddThrowsOnNone()
        {
            // arrange
            var value = new DateTime(2021, 11, 5, 19, 11, 8, 242);

            // act
            void Test() => value.Add(KlineInterval.None);

            // assert
            Assert.Throws<ArgumentOutOfRangeException>("interval", Test);
        }

        public static DateTime AddValue { get; } = new DateTime(2021, 11, 5, 19, 11, 8, 242);

        public static IEnumerable<object[]> AddTestData { get; } = new[]
        {
            new object[] { KlineInterval.Minutes1, AddValue, AddValue.AddMinutes(1) },
            new object[] { KlineInterval.Minutes3, AddValue, AddValue.AddMinutes(3) },
            new object[] { KlineInterval.Minutes5, AddValue, AddValue.AddMinutes(5) },
            new object[] { KlineInterval.Minutes15, AddValue, AddValue.AddMinutes(15) },
            new object[] { KlineInterval.Minutes30, AddValue, AddValue.AddMinutes(30) },
            new object[] { KlineInterval.Hours1, AddValue, AddValue.AddHours(1) },
            new object[] { KlineInterval.Hours2, AddValue, AddValue.AddHours(2) },
            new object[] { KlineInterval.Hours4, AddValue, AddValue.AddHours(4) },
            new object[] { KlineInterval.Hours6, AddValue, AddValue.AddHours(6) },
            new object[] { KlineInterval.Hours8, AddValue, AddValue.AddHours(8) },
            new object[] { KlineInterval.Hours12, AddValue, AddValue.AddHours(12) },
            new object[] { KlineInterval.Days1, AddValue, AddValue.AddDays(1) },
            new object[] { KlineInterval.Days3, AddValue, AddValue.AddDays(3) },
            new object[] { KlineInterval.Weeks1, AddValue, AddValue.AddDays(7) },
            new object[] { KlineInterval.Months1, AddValue, AddValue.AddMonths(1) }
        };

        [Theory]
        [MemberData(nameof(AddTestData))]
        public void Adds(KlineInterval interval, DateTime value, DateTime expected)
        {
            // act
            var result = value.Add(interval);

            // assert
            Assert.Equal(expected, result);
        }

        public static DateTime SubtractValue { get; } = new DateTime(2021, 11, 5, 19, 11, 8, 242);

        public static IEnumerable<object[]> SubtractTestData { get; } = new[]
        {
            new object[] { KlineInterval.Minutes1, SubtractValue, SubtractValue.AddMinutes(-1) },
            new object[] { KlineInterval.Minutes3, SubtractValue, SubtractValue.AddMinutes(-3) },
            new object[] { KlineInterval.Minutes5, SubtractValue, SubtractValue.AddMinutes(-5) },
            new object[] { KlineInterval.Minutes15, SubtractValue, SubtractValue.AddMinutes(-15) },
            new object[] { KlineInterval.Minutes30, SubtractValue, SubtractValue.AddMinutes(-30) },
            new object[] { KlineInterval.Hours1, SubtractValue, SubtractValue.AddHours(-1) },
            new object[] { KlineInterval.Hours2, SubtractValue, SubtractValue.AddHours(-2) },
            new object[] { KlineInterval.Hours4, SubtractValue, SubtractValue.AddHours(-4) },
            new object[] { KlineInterval.Hours6, SubtractValue, SubtractValue.AddHours(-6) },
            new object[] { KlineInterval.Hours8, SubtractValue, SubtractValue.AddHours(-8) },
            new object[] { KlineInterval.Hours12, SubtractValue, SubtractValue.AddHours(-12) },
            new object[] { KlineInterval.Days1, SubtractValue, SubtractValue.AddDays(-1) },
            new object[] { KlineInterval.Days3, SubtractValue, SubtractValue.AddDays(-3) },
            new object[] { KlineInterval.Weeks1, SubtractValue, SubtractValue.AddDays(-7) },
            new object[] { KlineInterval.Months1, SubtractValue, SubtractValue.AddMonths(-1) }
        };

        [Theory]
        [MemberData(nameof(SubtractTestData))]
        public void Subtracts(KlineInterval interval, DateTime value, DateTime expected)
        {
            // act
            var result = value.Subtract(interval);

            // assert
            Assert.Equal(expected, result);
        }
    }
}