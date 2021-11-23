using Xunit;

namespace Outcompute.Trader.Core.Tests
{
    public class MathDTests
    {
        [Theory]
        [InlineData(30, 70, -0.1, 26)]
        [InlineData(30, 70, +0.0, 30)]
        [InlineData(30, 70, +0.1, 34)]
        [InlineData(30, 70, +0.2, 38)]
        [InlineData(30, 70, +0.3, 42)]
        [InlineData(30, 70, +0.4, 46)]
        [InlineData(30, 70, +0.5, 50)]
        [InlineData(30, 70, +0.6, 54)]
        [InlineData(30, 70, +0.7, 58)]
        [InlineData(30, 70, +0.8, 62)]
        [InlineData(30, 70, +0.9, 66)]
        [InlineData(30, 70, +1.0, 70)]
        [InlineData(30, 70, +1.1, 74)]
        [InlineData(30, 30, +0.5, 30)]
        public void Lerps(decimal start, decimal end, decimal ratio, decimal expected)
        {
            // act
            var result = MathD.Lerp(start, end, ratio);

            // assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(30, 70, 26, -0.1)]
        [InlineData(30, 70, 30, +0.0)]
        [InlineData(30, 70, 34, +0.1)]
        [InlineData(30, 70, 38, +0.2)]
        [InlineData(30, 70, 42, +0.3)]
        [InlineData(30, 70, 46, +0.4)]
        [InlineData(30, 70, 50, +0.5)]
        [InlineData(30, 70, 54, +0.6)]
        [InlineData(30, 70, 58, +0.7)]
        [InlineData(30, 70, 62, +0.8)]
        [InlineData(30, 70, 66, +0.9)]
        [InlineData(30, 70, 70, +1.0)]
        [InlineData(30, 70, 74, +1.1)]
        [InlineData(30, 30, 30, +0.0)]
        [InlineData(30, 30, 25, +0.0)]
        [InlineData(30, 30, 35, +0.0)]
        public void InverseLerps(decimal start, decimal end, decimal value, decimal expected)
        {
            // act
            var result = MathD.InverseLerp(start, end, value);

            // assert
            Assert.Equal(expected, result);
        }

        [Theory]
        // 30-70 => 0-100
        [InlineData(30, 70, 25, 0, 100, -12.5)]
        [InlineData(30, 70, 30, 0, 100, 0.0)]
        [InlineData(30, 70, 35, 0, 100, 12.5)]
        [InlineData(30, 70, 40, 0, 100, 25.0)]
        [InlineData(30, 70, 45, 0, 100, 37.5)]
        [InlineData(30, 70, 50, 0, 100, 50.0)]
        [InlineData(30, 70, 55, 0, 100, 62.5)]
        [InlineData(30, 70, 60, 0, 100, 75.0)]
        [InlineData(30, 70, 65, 0, 100, 87.5)]
        [InlineData(30, 70, 70, 0, 100, 100.0)]
        [InlineData(30, 70, 75, 0, 100, 112.5)]
        // 30-70 => 10-110
        [InlineData(30, 70, 25, 10, 110, -2.5)]
        [InlineData(30, 70, 30, 10, 110, 10.0)]
        [InlineData(30, 70, 35, 10, 110, 22.5)]
        [InlineData(30, 70, 40, 10, 110, 35.0)]
        [InlineData(30, 70, 45, 10, 110, 47.5)]
        [InlineData(30, 70, 50, 10, 110, 60.0)]
        [InlineData(30, 70, 55, 10, 110, 72.5)]
        [InlineData(30, 70, 60, 10, 110, 85.0)]
        [InlineData(30, 70, 65, 10, 110, 97.5)]
        [InlineData(30, 70, 70, 10, 110, 110.0)]
        [InlineData(30, 70, 75, 10, 110, 122.5)]
        public void LerpsBetween(decimal sourceStart, decimal sourceEnd, decimal sourceValue, decimal targetStart, decimal targetEnd, decimal expected)
        {
            // act
            var result = MathD.LerpBetween(sourceStart, sourceEnd, sourceValue, targetStart, targetEnd);

            // assert
            Assert.Equal(expected, result);
        }
    }
}