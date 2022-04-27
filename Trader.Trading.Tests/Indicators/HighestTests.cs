using Outcompute.Trader.Indicators;

namespace Outcompute.Trader.Trading.Tests.Indicators;

public static class HighestTests
{
    [Fact]
    public static void YieldsOutput()
    {
        // arrange
        using var indicator = Indicator
            .Identity<decimal?>(1, 2, 3, 2, 3, 4, 5, 4, 5, 6, 7)
            .Highest(3);

        // arrange
        Assert.Collection(indicator,
            x => Assert.Null(x),
            x => Assert.Null(x),
            x => Assert.Equal(3, x),
            x => Assert.Equal(3, x),
            x => Assert.Equal(3, x),
            x => Assert.Equal(4, x),
            x => Assert.Equal(5, x),
            x => Assert.Equal(5, x),
            x => Assert.Equal(5, x),
            x => Assert.Equal(6, x),
            x => Assert.Equal(7, x));
    }
}