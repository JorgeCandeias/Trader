using Outcompute.Trader.Trading.Indicators;

namespace Outcompute.Trader.Trading.Tests.Indicators;

public class TrueRangeTests
{
    [Fact]
    public void YieldsTrueRange()
    {
        // act
        using var indicator = new TrueRange()
        {
            new HLC(10, 5, 6),
            new HLC(11, 4, 8),
            new HLC(7, 1, 5)
        };

        // assert
        Assert.Collection(indicator,
            x => Assert.Equal(5, x),
            x => Assert.Equal(7, x),
            x => Assert.Equal(7, x));
    }
}