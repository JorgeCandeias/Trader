using Outcompute.Trader.Trading.Indicators;

namespace Outcompute.Trader.Trading.Tests.Indicators;

public static class LowestTests
{
    [Fact]
    public static void YieldsOutput()
    {
        // arrange
        using var indicator = new Lowest(3);

        // act
        indicator.AddRange(new decimal?[] { 1, 2, 3, 2, 3, 4, 5, 4, 5, 6, 7 });

        // arrange
        Assert.Collection(indicator,
            x => Assert.Null(x),
            x => Assert.Null(x),
            x => Assert.Equal(1, x),
            x => Assert.Equal(2, x),
            x => Assert.Equal(2, x),
            x => Assert.Equal(2, x),
            x => Assert.Equal(3, x),
            x => Assert.Equal(4, x),
            x => Assert.Equal(4, x),
            x => Assert.Equal(4, x),
            x => Assert.Equal(5, x));
    }
}