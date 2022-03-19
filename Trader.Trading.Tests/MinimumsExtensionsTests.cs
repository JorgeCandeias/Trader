namespace Outcompute.Trader.Trading.Tests;

public class MinimumsExtensionsTests
{
    [Theory]
    [MemberData(nameof(TestCases))]
    public void Enumerates(decimal?[] source, int other, decimal?[] expected)
    {
        var result = source.Minimums(other).ToArray();

        for (var i = 0; i < source.Length; i++)
        {
            Assert.Equal(expected[i], result[i]);
        }
    }

    public static readonly IEnumerable<object[]> TestCases = new object[][]
    {
        new object[] { new decimal?[] { -3, -2, -1, 0, 1, 2, 3 }, -3, new decimal?[] { -3, -3, -3, -3, -3, -3, -3 } },
        new object[] { new decimal?[] { -3, -2, -1, 0, 1, 2, 3 }, -2, new decimal?[] { -3, -2, -2, -2, -2, -2, -2 } },
        new object[] { new decimal?[] { -3, -2, -1, 0, 1, 2, 3 }, -1, new decimal?[] { -3, -2, -1, -1, -1, -1, -1 } },
        new object[] { new decimal?[] { -3, -2, -1, 0, 1, 2, 3 }, 0, new decimal?[] { -3, -2, -1, 0, 0, 0, 0 } },
        new object[] { new decimal?[] { -3, -2, -1, 0, 1, 2, 3 }, 1, new decimal?[] { -3, -2, -1, 0, 1, 1, 1 } },
        new object[] { new decimal?[] { -3, -2, -1, 0, 1, 2, 3 }, 2, new decimal?[] { -3, -2, -1, 0, 1, 2, 2 } },
        new object[] { new decimal?[] { -3, -2, -1, 0, 1, 2, 3 }, 3, new decimal?[] { -3, -2, -1, 0, 1, 2, 3 } },
    };
}