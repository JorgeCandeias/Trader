using Outcompute.Trader.Trading.Indicators;

namespace Outcompute.Trader.Trading.Tests.Indicators;

public static class IdentityTests
{
    [Fact]
    public static void YieldsEmptyOutput()
    {
        // act
        using var indicator = new Identity<int>();

        // assert
        Assert.Empty(indicator);
    }

    [Fact]
    public static void YieldsOutput()
    {
        // act
        using var indicator = new Identity<int>()
        {
            1, -1, 2, -3, 5, -8, 13, -21, 34, -55, 89, -144
        };

        // assert
        Assert.Collection(indicator,
            x => Assert.Equal(1, x),
            x => Assert.Equal(-1, x),
            x => Assert.Equal(2, x),
            x => Assert.Equal(-3, x),
            x => Assert.Equal(5, x),
            x => Assert.Equal(-8, x),
            x => Assert.Equal(13, x),
            x => Assert.Equal(-21, x),
            x => Assert.Equal(34, x),
            x => Assert.Equal(-55, x),
            x => Assert.Equal(89, x),
            x => Assert.Equal(-144, x));
    }

    [Fact]
    public static void YieldsUpdate()
    {
        // arrange
        using var indicator = new Identity<int>()
        {
            1, -1, 2, -3, 5, -8, 13, -21, 34, -55, 89, -144
        };

        // act
        indicator.Update(2, -999);

        // assert
        Assert.Collection(indicator,
            x => Assert.Equal(1, x),
            x => Assert.Equal(-1, x),
            x => Assert.Equal(-999, x),
            x => Assert.Equal(-3, x),
            x => Assert.Equal(5, x),
            x => Assert.Equal(-8, x),
            x => Assert.Equal(13, x),
            x => Assert.Equal(-21, x),
            x => Assert.Equal(34, x),
            x => Assert.Equal(-55, x),
            x => Assert.Equal(89, x),
            x => Assert.Equal(-144, x));
    }

    [Fact]
    public static void YieldsOutOfRangeUpdate()
    {
        // arrange
        using var indicator = new Identity<int>()
        {
            1, -1, 2, -3, 5
        };

        // act
        indicator.Update(9, -999);

        // assert
        Assert.Collection(indicator,
            x => Assert.Equal(1, x),
            x => Assert.Equal(-1, x),
            x => Assert.Equal(2, x),
            x => Assert.Equal(-3, x),
            x => Assert.Equal(5, x),
            x => Assert.Equal(default, x),
            x => Assert.Equal(default, x),
            x => Assert.Equal(default, x),
            x => Assert.Equal(default, x),
            x => Assert.Equal(-999, x));
    }
}