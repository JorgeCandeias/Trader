using Xunit;

namespace Outcompute.Trader.Core.Tests
{
    public class MathDTests
    {
        [Theory]
        [InlineData(-0.1, 0.0)]
        [InlineData(0.0, 0.0)]
        [InlineData(0.1, 0.1)]
        [InlineData(0.5, 0.5)]
        [InlineData(0.9, 0.9)]
        [InlineData(1.0, 1.0)]
        [InlineData(1.1, 1.0)]
        public void Clamps01(decimal value, decimal expected)
        {
            // act
            var result = MathD.Clamp01(value);

            // assert
            Assert.Equal(expected, result);
        }

        [Theory]
        // low to high range
        [InlineData(30, 70, -0.1, 30)]
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
        [InlineData(30, 70, +1.1, 70)]
        // high to low range
        [InlineData(70, 30, -0.1, 70)]
        [InlineData(70, 30, +0.0, 70)]
        [InlineData(70, 30, +0.1, 66)]
        [InlineData(70, 30, +0.2, 62)]
        [InlineData(70, 30, +0.3, 58)]
        [InlineData(70, 30, +0.4, 54)]
        [InlineData(70, 30, +0.5, 50)]
        [InlineData(70, 30, +0.6, 46)]
        [InlineData(70, 30, +0.7, 42)]
        [InlineData(70, 30, +0.8, 38)]
        [InlineData(70, 30, +0.9, 34)]
        [InlineData(70, 30, +1.0, 30)]
        [InlineData(70, 30, +1.1, 30)]
        // zero range
        [InlineData(50, 50, -0.5, 50)]
        [InlineData(50, 50, +0.0, 50)]
        [InlineData(50, 50, +0.5, 50)]
        [InlineData(50, 50, +1.0, 50)]
        [InlineData(50, 50, +1.5, 50)]
        public void Lerps(decimal start, decimal end, decimal ratio, decimal expected)
        {
            // act
            var result = MathD.Lerp(start, end, ratio);

            // assert
            Assert.Equal(expected, result);
        }

        [Theory]
        // low to high range
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
        // high to low range
        [InlineData(70, 30, -0.1, 74)]
        [InlineData(70, 30, +0.0, 70)]
        [InlineData(70, 30, +0.1, 66)]
        [InlineData(70, 30, +0.2, 62)]
        [InlineData(70, 30, +0.3, 58)]
        [InlineData(70, 30, +0.4, 54)]
        [InlineData(70, 30, +0.5, 50)]
        [InlineData(70, 30, +0.6, 46)]
        [InlineData(70, 30, +0.7, 42)]
        [InlineData(70, 30, +0.8, 38)]
        [InlineData(70, 30, +0.9, 34)]
        [InlineData(70, 30, +1.0, 30)]
        [InlineData(70, 30, +1.1, 26)]
        // zero range
        [InlineData(50, 50, -0.5, 50)]
        [InlineData(50, 50, +0.0, 50)]
        [InlineData(50, 50, +0.5, 50)]
        [InlineData(50, 50, +1.0, 50)]
        [InlineData(50, 50, +1.5, 50)]
        public void LerpsUnclamped(decimal start, decimal end, decimal ratio, decimal expected)
        {
            // act
            var result = MathD.LerpUnclamped(start, end, ratio);

            // assert
            Assert.Equal(expected, result);
        }

        [Theory]
        // low to high range
        [InlineData(30, 70, 26, +0.0)]
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
        [InlineData(30, 70, 74, +1.0)]
        // high to low range
        [InlineData(70, 30, 74, +0.0)]
        [InlineData(70, 30, 70, +0.0)]
        [InlineData(70, 30, 66, +0.1)]
        [InlineData(70, 30, 62, +0.2)]
        [InlineData(70, 30, 58, +0.3)]
        [InlineData(70, 30, 54, +0.4)]
        [InlineData(70, 30, 50, +0.5)]
        [InlineData(70, 30, 46, +0.6)]
        [InlineData(70, 30, 42, +0.7)]
        [InlineData(70, 30, 38, +0.8)]
        [InlineData(70, 30, 34, +0.9)]
        [InlineData(70, 30, 30, +1.0)]
        [InlineData(70, 30, 26, +1.0)]
        // zero range
        [InlineData(50, 50, 45, +0.0)]
        [InlineData(50, 50, 50, +0.0)]
        [InlineData(50, 50, 55, +0.0)]
        public void InverseLerps(decimal start, decimal end, decimal value, decimal expected)
        {
            // act
            var result = MathD.InverseLerp(start, end, value);

            // assert
            Assert.Equal(expected, result);
        }

        [Theory]
        // low to high range
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
        // high to low range
        [InlineData(70, 30, 74, -0.1)]
        [InlineData(70, 30, 70, +0.0)]
        [InlineData(70, 30, 66, +0.1)]
        [InlineData(70, 30, 62, +0.2)]
        [InlineData(70, 30, 58, +0.3)]
        [InlineData(70, 30, 54, +0.4)]
        [InlineData(70, 30, 50, +0.5)]
        [InlineData(70, 30, 46, +0.6)]
        [InlineData(70, 30, 42, +0.7)]
        [InlineData(70, 30, 38, +0.8)]
        [InlineData(70, 30, 34, +0.9)]
        [InlineData(70, 30, 30, +1.0)]
        [InlineData(70, 30, 26, +1.1)]
        // zero range
        [InlineData(50, 50, 45, +0.0)]
        [InlineData(50, 50, 50, +0.0)]
        [InlineData(50, 50, 55, +0.0)]
        public void InverseLerpsUnclamped(decimal start, decimal end, decimal value, decimal expected)
        {
            // act
            var result = MathD.InverseLerpUnclamped(start, end, value);

            // assert
            Assert.Equal(expected, result);
        }

        [Theory]
        // low-high => low-high
        [InlineData(30, 70, 25, 10, 110, 10.0)]
        [InlineData(30, 70, 30, 10, 110, 10.0)]
        [InlineData(30, 70, 35, 10, 110, 22.5)]
        [InlineData(30, 70, 40, 10, 110, 35.0)]
        [InlineData(30, 70, 45, 10, 110, 47.5)]
        [InlineData(30, 70, 50, 10, 110, 60.0)]
        [InlineData(30, 70, 55, 10, 110, 72.5)]
        [InlineData(30, 70, 60, 10, 110, 85.0)]
        [InlineData(30, 70, 65, 10, 110, 97.5)]
        [InlineData(30, 70, 70, 10, 110, 110.0)]
        [InlineData(30, 70, 75, 10, 110, 110.0)]
        // low-high => high-low
        [InlineData(30, 70, 25, 110, 10, 110.0)]
        [InlineData(30, 70, 30, 110, 10, 110.0)]
        [InlineData(30, 70, 35, 110, 10, 97.5)]
        [InlineData(30, 70, 40, 110, 10, 85.0)]
        [InlineData(30, 70, 45, 110, 10, 72.5)]
        [InlineData(30, 70, 50, 110, 10, 60.0)]
        [InlineData(30, 70, 55, 110, 10, 47.5)]
        [InlineData(30, 70, 60, 110, 10, 35.0)]
        [InlineData(30, 70, 65, 110, 10, 22.5)]
        [InlineData(30, 70, 70, 110, 10, 10.0)]
        [InlineData(30, 70, 75, 110, 10, 10.0)]
        // low-high => zero range
        [InlineData(30, 70, 40, 10, 10, 10.0)]
        [InlineData(30, 70, 50, 10, 10, 10.0)]
        [InlineData(30, 70, 60, 10, 10, 10.0)]
        // high-low => low-high
        [InlineData(70, 30, 75, 10, 110, 10.0)]
        [InlineData(70, 30, 70, 10, 110, 10.0)]
        [InlineData(70, 30, 65, 10, 110, 22.5)]
        [InlineData(70, 30, 60, 10, 110, 35.0)]
        [InlineData(70, 30, 55, 10, 110, 47.5)]
        [InlineData(70, 30, 50, 10, 110, 60.0)]
        [InlineData(70, 30, 45, 10, 110, 72.5)]
        [InlineData(70, 30, 40, 10, 110, 85.0)]
        [InlineData(70, 30, 35, 10, 110, 97.5)]
        [InlineData(70, 30, 30, 10, 110, 110.0)]
        [InlineData(70, 30, 25, 10, 110, 110.0)]
        // high-low => high-low
        [InlineData(70, 30, 75, 110, 10, 110.0)]
        [InlineData(70, 30, 70, 110, 10, 110.0)]
        [InlineData(70, 30, 65, 110, 10, 97.5)]
        [InlineData(70, 30, 60, 110, 10, 85.0)]
        [InlineData(70, 30, 55, 110, 10, 72.5)]
        [InlineData(70, 30, 50, 110, 10, 60.0)]
        [InlineData(70, 30, 45, 110, 10, 47.5)]
        [InlineData(70, 30, 40, 110, 10, 35.0)]
        [InlineData(70, 30, 35, 110, 10, 22.5)]
        [InlineData(70, 30, 30, 110, 10, 10.0)]
        [InlineData(70, 30, 25, 110, 10, 10.0)]
        // high-low => zero range
        [InlineData(70, 30, 60, 10, 10, 10.0)]
        [InlineData(70, 30, 50, 10, 10, 10.0)]
        [InlineData(70, 30, 40, 10, 10, 10.0)]
        // zero range => low-high
        [InlineData(50, 50, 40, 10, 110, 10.0)]
        [InlineData(50, 50, 50, 10, 110, 10.0)]
        [InlineData(50, 50, 60, 10, 110, 10.0)]
        // zero range => high-low
        [InlineData(50, 50, 40, 110, 10, 110.0)]
        [InlineData(50, 50, 50, 110, 10, 110.0)]
        [InlineData(50, 50, 60, 110, 10, 110.0)]
        // zero range => zero range
        [InlineData(50, 50, 40, 110, 110, 110.0)]
        [InlineData(50, 50, 50, 110, 110, 110.0)]
        [InlineData(50, 50, 60, 110, 110, 110.0)]
        public void LerpsBetween(decimal sourceStart, decimal sourceEnd, decimal sourceValue, decimal targetStart, decimal targetEnd, decimal expected)
        {
            // act
            var result = MathD.LerpBetween(sourceStart, sourceEnd, sourceValue, targetStart, targetEnd);

            // assert
            Assert.Equal(expected, result);
        }

        [Theory]
        // low-high => low-high
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
        // low-high => high-low
        [InlineData(30, 70, 25, 110, 10, 122.5)]
        [InlineData(30, 70, 30, 110, 10, 110.0)]
        [InlineData(30, 70, 35, 110, 10, 97.5)]
        [InlineData(30, 70, 40, 110, 10, 85.0)]
        [InlineData(30, 70, 45, 110, 10, 72.5)]
        [InlineData(30, 70, 50, 110, 10, 60.0)]
        [InlineData(30, 70, 55, 110, 10, 47.5)]
        [InlineData(30, 70, 60, 110, 10, 35.0)]
        [InlineData(30, 70, 65, 110, 10, 22.5)]
        [InlineData(30, 70, 70, 110, 10, 10.0)]
        [InlineData(30, 70, 75, 110, 10, -2.5)]
        // low-high => zero range
        [InlineData(30, 70, 40, 10, 10, 10.0)]
        [InlineData(30, 70, 50, 10, 10, 10.0)]
        [InlineData(30, 70, 60, 10, 10, 10.0)]
        // high-low => low-high
        [InlineData(70, 30, 75, 10, 110, -2.5)]
        [InlineData(70, 30, 70, 10, 110, 10.0)]
        [InlineData(70, 30, 65, 10, 110, 22.5)]
        [InlineData(70, 30, 60, 10, 110, 35.0)]
        [InlineData(70, 30, 55, 10, 110, 47.5)]
        [InlineData(70, 30, 50, 10, 110, 60.0)]
        [InlineData(70, 30, 45, 10, 110, 72.5)]
        [InlineData(70, 30, 40, 10, 110, 85.0)]
        [InlineData(70, 30, 35, 10, 110, 97.5)]
        [InlineData(70, 30, 30, 10, 110, 110.0)]
        [InlineData(70, 30, 25, 10, 110, 122.5)]
        // high-low => high-low
        [InlineData(70, 30, 75, 110, 10, 122.5)]
        [InlineData(70, 30, 70, 110, 10, 110.0)]
        [InlineData(70, 30, 65, 110, 10, 97.5)]
        [InlineData(70, 30, 60, 110, 10, 85.0)]
        [InlineData(70, 30, 55, 110, 10, 72.5)]
        [InlineData(70, 30, 50, 110, 10, 60.0)]
        [InlineData(70, 30, 45, 110, 10, 47.5)]
        [InlineData(70, 30, 40, 110, 10, 35.0)]
        [InlineData(70, 30, 35, 110, 10, 22.5)]
        [InlineData(70, 30, 30, 110, 10, 10.0)]
        [InlineData(70, 30, 25, 110, 10, -2.5)]
        // high-low => zero range
        [InlineData(70, 30, 60, 10, 10, 10.0)]
        [InlineData(70, 30, 50, 10, 10, 10.0)]
        [InlineData(70, 30, 40, 10, 10, 10.0)]
        // zero range => low-high
        [InlineData(50, 50, 40, 10, 110, 10.0)]
        [InlineData(50, 50, 50, 10, 110, 10.0)]
        [InlineData(50, 50, 60, 10, 110, 10.0)]
        // zero range => high-low
        [InlineData(50, 50, 40, 110, 10, 110.0)]
        [InlineData(50, 50, 50, 110, 10, 110.0)]
        [InlineData(50, 50, 60, 110, 10, 110.0)]
        // zero range => zero range
        [InlineData(50, 50, 40, 110, 110, 110.0)]
        [InlineData(50, 50, 50, 110, 110, 110.0)]
        [InlineData(50, 50, 60, 110, 110, 110.0)]
        public void LerpsBetweenUnclamped(decimal sourceStart, decimal sourceEnd, decimal sourceValue, decimal targetStart, decimal targetEnd, decimal expected)
        {
            // act
            var result = MathD.LerpBetweenUnclamped(sourceStart, sourceEnd, sourceValue, targetStart, targetEnd);

            // assert
            Assert.Equal(expected, result);
        }
    }
}