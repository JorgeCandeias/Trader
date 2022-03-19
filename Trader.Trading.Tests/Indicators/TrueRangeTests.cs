using Outcompute.Trader.Trading.Indicators;

namespace Outcompute.Trader.Trading.Tests.Indicators;

public class TrueRangeTests
{
    [Fact]
    public void YieldsTrueRange()
    {
        // act
        using var source = new Identity<HLC>
        {
            new HLC(10, 5, 6),
            new HLC(11, 4, 8),
            new HLC(7, 1, 5)
        };
        using var indicator = new TrueRange(source);

        // assert
        Assert.Collection(indicator,
            x => Assert.Null(x),
            x => Assert.Equal(7, x),
            x => Assert.Equal(7, x));
    }
}