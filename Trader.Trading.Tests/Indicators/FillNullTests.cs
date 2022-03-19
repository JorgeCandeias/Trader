using Outcompute.Trader.Trading.Indicators;

namespace Outcompute.Trader.Trading.Tests.Indicators;

public static class FillNullTests
{
    [Fact]
    public static void YieldsOutput()
    {
        // act
        using var indicator = Indicator
            .Identity<decimal?>(null, 2, 3, null, 4, 5, null, null, 8, 9, null)
            .FillNull();

        // assert
        Assert.Collection(indicator,
            x => Assert.Null(x),
            x => Assert.Equal(2, x),
            x => Assert.Equal(3, x),
            x => Assert.Equal(3, x),
            x => Assert.Equal(4, x),
            x => Assert.Equal(5, x),
            x => Assert.Equal(5, x),
            x => Assert.Equal(5, x),
            x => Assert.Equal(8, x),
            x => Assert.Equal(9, x),
            x => Assert.Equal(9, x));
    }
}