namespace Outcompute.Trader.Trading.Tests;

public class MovingMaxTests
{
    [Theory]
    [MemberData(nameof(TestData))]
    public void Evaluates(decimal[] source, decimal[] expected, int periods)
    {
        var momentum = source.MovingMax(periods).ToArray();

        for (var i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], momentum[i]);
        }
    }

    public static readonly IEnumerable<object[]> TestData = new object[][]{
        new object[] { new decimal[] { 1, 2, 3, 4, 5, 6 }, new decimal[] { 1, 2, 3, 4, 5, 6 }, 1 },
        new object[] { new decimal[] { 1, 2, 3, 4, 5, 6 }, new decimal[] { 1, 2, 3, 4, 5, 6 }, 2 },
        new object[] { new decimal[] { 1, 2, 3, 4, 5, 6 }, new decimal[] { 1, 2, 3, 4, 5, 6 }, 3 },
        new object[] { new decimal[] { 1, 2, 3, 4, 5, 6 }, new decimal[] { 1, 2, 3, 4, 5, 6 }, 4 },
        new object[] { new decimal[] { 1, 2, 3, 4, 5, 6 }, new decimal[] { 1, 2, 3, 4, 5, 6 }, 5 },
        new object[] { new decimal[] { 1, 2, 3, 4, 5, 6 }, new decimal[] { 1, 2, 3, 4, 5, 6 }, 6 },
        new object[] { new decimal[] { 6, 5, 4, 3, 2, 1 }, new decimal[] { 6, 5, 4, 3, 2, 1 }, 1 },
        new object[] { new decimal[] { 6, 5, 4, 3, 2, 1 }, new decimal[] { 6, 6, 5, 4, 3, 2 }, 2 },
        new object[] { new decimal[] { 6, 5, 4, 3, 2, 1 }, new decimal[] { 6, 6, 6, 5, 4, 3 }, 3 },
        new object[] { new decimal[] { 6, 5, 4, 3, 2, 1 }, new decimal[] { 6, 6, 6, 6, 5, 4 }, 4 },
        new object[] { new decimal[] { 6, 5, 4, 3, 2, 1 }, new decimal[] { 6, 6, 6, 6, 6, 5 }, 5 },
        new object[] { new decimal[] { 6, 5, 4, 3, 2, 1 }, new decimal[] { 6, 6, 6, 6, 6, 6 }, 6 },
    };
}