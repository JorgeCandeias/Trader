namespace Outcompute.Trader.Trading.Tests;

public class MomentumTests
{
    [Theory]
    [MemberData(nameof(TestData))]
    public void Evaluates(decimal?[] source, decimal?[] expected, int periods)
    {
        var momentum = source.Momentum(periods).ToArray();

        for (var i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], momentum[i]);
        }
    }

    public static readonly IEnumerable<object[]> TestData = new object[][]{
        new object[] { new decimal?[] { 1, 1, 1, 1, 1, 1 }, new decimal?[] { null, 0, 0, 0, 0, 0 }, 1 },
        new object[] { new decimal?[] { 1, 2, 3, 4, 5, 6 }, new decimal?[] { null, 1, 1, 1, 1, 1 }, 1 },
        new object[] { new decimal?[] { 1, 2, 3, 5, 8, 13 }, new decimal?[] { null, 1, 1, 2, 3, 5 }, 1 },

        new object[] { new decimal?[] { 1, 3, 2, 13, 8, 5 }, new decimal?[] { null, 2, -1, 11, -5, -3 }, 1 },

        new object[] { new decimal?[] { 1, 1, 1, 1, 1, 1 }, new decimal?[] { null, null, 0, 0, 0, 0 }, 2 },
        new object[] { new decimal?[] { 1, 2, 3, 4, 5, 6 }, new decimal?[] { null, null, 2, 2, 2, 2 }, 2 },
        new object[] { new decimal?[] { 1, 2, 3, 5, 8, 13 }, new decimal?[] { null, null, 2, 3, 5, 8 }, 2 },

        new object[] { new decimal?[] { 1, 3, 2, 13, 8, 5 }, new decimal?[] { null, null, 1, 10, 6, -8 }, 2 },

        new object[] { new decimal?[] { 1, 1, 1, 1, 1, 1 }, new decimal?[] { null, null, null, 0, 0, 0 }, 3 },
        new object[] { new decimal?[] { 1, 2, 3, 4, 5, 6 }, new decimal?[] { null, null, null, 3, 3, 3 }, 3 },
        new object[] { new decimal?[] { 1, 2, 3, 5, 8, 13 }, new decimal?[] { null, null, null, 4, 6, 10 }, 3 },

        new object[] { new decimal?[] { 1, 3, 2, 13, 8, 5 }, new decimal?[] { null, null, null, 12, 5, 3 }, 3 },
    };
}