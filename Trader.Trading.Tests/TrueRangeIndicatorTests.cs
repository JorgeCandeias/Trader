using Outcompute.Trader.Trading.Indicators;

namespace Outcompute.Trader.Trading.Tests;

public class TrueRangeIndicatorTests
{
    [Fact]
    public void YieldsTrueRange()
    {
        // act
        var indicator = new TrueRangeIndicator()
        {
            (10, 5, 6), (11, 4, 8), (7, 1, 5)
        };

        // assert
        Assert.Collection(indicator,
            x => Assert.Equal(5, x),
            x => Assert.Equal(7, x),
            x => Assert.Equal(7, x));
    }
}