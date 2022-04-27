using Outcompute.Trader.Indicators;

namespace Outcompute.Trader.Trading.Tests.Indicators;

public static class LowestTests
{
    [Fact]
    public static void YieldsOutput()
    {
        // arrange
        using var identity = new Identity<decimal?> { 1, 2, 3, 2, 3, 4, 5, 4, 5, 6, 7 };
        using var indicator = new Lowest(identity, 3);

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