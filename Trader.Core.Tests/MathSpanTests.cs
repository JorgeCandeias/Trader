using System;
using Xunit;

namespace Outcompute.Trader.Core.Tests
{
    public class MathSpanTests
    {
        private class MyValue : IComparable<MyValue>
        {
            public MyValue(int value)
            {
                Value = value;
            }

            public int Value { get; }

            public int CompareTo(MyValue? other)
            {
                return other is null ? 1 : Value.CompareTo(other.Value);
            }
        }

        [Fact]
        public void MaxIntThrowsOnEmptyValues()
        {
            // arrange
            var values = Array.Empty<int>();

            // act
            void Test() => MathSpan.Max(values.AsSpan());

            // assert
            Assert.Throws<ArgumentOutOfRangeException>("values", Test);
        }

        [Fact]
        public void MaxIntReturnsMax()
        {
            // arrange
            var values = new int[] { 1, 3, 2 };

            // act
            var result = MathSpan.Max(values.AsSpan());

            // assert
            Assert.Equal(3, result);
        }

        [Fact]
        public void MaxDecimalThrowsOnEmptyValues()
        {
            // arrange
            var values = Array.Empty<decimal>();

            // act
            void Test() => MathSpan.Max(values.AsSpan());

            // assert
            Assert.Throws<ArgumentOutOfRangeException>("values", Test);
        }

        [Fact]
        public void MaxDecimalReturnsMax()
        {
            // arrange
            var values = new decimal[] { 1.1m, 3.3m, 2.2m };

            // act
            var result = MathSpan.Max(values.AsSpan());

            // assert
            Assert.Equal(3.3m, result);
        }

        [Fact]
        public void MaxOfTThrowsOnEmptyValues()
        {
            // arrange
            var values = Array.Empty<MyValue>();

            // act
            void Test() => MathSpan.Max<MyValue>(values.AsSpan());

            // assert
            Assert.Throws<ArgumentOutOfRangeException>("values", Test);
        }

        [Fact]
        public void MaxOfTReturnsMax()
        {
            // arrange
            var value1 = new MyValue(1);
            var value2 = new MyValue(2);
            var value3 = new MyValue(3);
            var values = new MyValue[] { value1, value3, value2 };

            // act
            var result = MathSpan.Max<MyValue>(values.AsSpan());

            // assert
            Assert.Same(value3, result);
        }

        [Fact]
        public void MinIntThrowsOnEmptyValues()
        {
            // arrange
            var values = Array.Empty<int>();

            // act
            void Test() => MathSpan.Min(values.AsSpan());

            // assert
            Assert.Throws<ArgumentOutOfRangeException>("values", Test);
        }

        [Fact]
        public void MinIntReturnsMin()
        {
            // arrange
            var values = new int[] { 1, 3, 2 };

            // act
            var result = MathSpan.Min(values.AsSpan());

            // assert
            Assert.Equal(1, result);
        }

        [Fact]
        public void MinDecimalThrowsOnEmptyValues()
        {
            // arrange
            var values = Array.Empty<decimal>();

            // act
            void Test() => MathSpan.Min(values.AsSpan());

            // assert
            Assert.Throws<ArgumentOutOfRangeException>("values", Test);
        }

        [Fact]
        public void MinDecimalReturnsMin()
        {
            // arrange
            var values = new decimal[] { 1.1m, 3.3m, 2.2m };

            // act
            var result = MathSpan.Min(values.AsSpan());

            // assert
            Assert.Equal(1.1m, result);
        }

        [Fact]
        public void MinOfTThrowsOnEmptyValues()
        {
            // arrange
            var values = Array.Empty<MyValue>();

            // act
            void Test() => MathSpan.Min<MyValue>(values.AsSpan());

            // assert
            Assert.Throws<ArgumentOutOfRangeException>("values", Test);
        }

        [Fact]
        public void MinOfTReturnsMin()
        {
            // arrange
            var value1 = new MyValue(1);
            var value2 = new MyValue(2);
            var value3 = new MyValue(3);
            var values = new MyValue[] { value1, value3, value2 };

            // act
            var result = MathSpan.Min<MyValue>(values.AsSpan());

            // assert
            Assert.Same(value1, result);
        }
    }
}