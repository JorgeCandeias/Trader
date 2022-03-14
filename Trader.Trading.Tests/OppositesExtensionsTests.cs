namespace Outcompute.Trader.Trading.Tests;

public class OppositesExtensionsTests
{
    [Theory]
    [MemberData(nameof(TestCases))]
    public void Enumerates(decimal?[] source, decimal?[] expected)
    {
        var result = source.Opposites().ToArray();

        for (var i = 0; i < source.Length; i++)
        {
            Assert.Equal(expected[i], result[i]);
        }
    }

    [Theory]
    [MemberData(nameof(TestCases))]
    public void EnumeratesWithSelector(decimal?[] source, decimal?[] expected)
    {
        var result = source.Opposites().ToArray();

        for (var i = 0; i < source.Length; i++)
        {
            Assert.Equal(expected[i], result[i]);
        }
    }

    public static readonly IEnumerable<object[]> TestCases = new object[][]
    {
        new object[] { new decimal?[] { -3, -2, -1, 0, 1, 2, 3 }, new decimal?[] { 3, 2, 1, 0, -1, -2, -3 } },
    };
}