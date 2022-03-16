using Outcompute.Trader.Trading.Indicators;

namespace Outcompute.Trader.Trading.Tests.Indicators;

public static class AbsTests
{
    [Fact]
    public static void YieldsEmptyOutput()
    {
        // act
        using var indicator = new Abs();

        // assert
        Assert.Empty(indicator);
    }

    [Fact]
    public static void YieldsAbsoluteOutput()
    {
        // act
        using var indicator = new Abs
        {
            1, -1, 2, -3, 5, -8, 13, -21, 34, -55, 89, -144
        };

        // assert
        Assert.True(indicator.SequenceEqual(new decimal?[] { 1, 1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 144 }));
    }

    [Fact]
    public static void UpdatesInPlace()
    {
        // arrange
        using var indicator = new Abs
        {
            1, -1, 2, -3, 5
        };

        // act
        indicator.Update(3, -4);

        // assert
        Assert.True(indicator.SequenceEqual(new decimal?[] { 1, 1, 2, 4, 5 }));
    }

    [Fact]
    public static void HandlesSource()
    {
        // arrange
        using var source = new Identity<decimal?>
        {
            1, -1, 2, -3, 5
        };

        // act
        using var indicator = new Abs(source);

        // assert
        Assert.True(indicator.SequenceEqual(new decimal?[] { 1, 1, 2, 3, 5 }));
    }

    [Fact]
    public static void UpdatesFromSource()
    {
        // arrange
        using var source = new Identity<decimal?>
        {
            1, -1, 2, -3, 5
        };
        using var indicator = new Abs(source);

        // act
        source.Update(3, -4);

        // assert
        Assert.True(indicator.SequenceEqual(new decimal?[] { 1, 1, 2, 4, 5 }));
    }
}