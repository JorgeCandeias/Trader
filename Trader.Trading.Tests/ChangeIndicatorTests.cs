using Outcompute.Trader.Trading.Indicators;

namespace Outcompute.Trader.Trading.Tests;

public class ChangeIndicatorTests
{
    [Fact]
    public void YieldsChange()
    {
        // act
        using var identity = Indicator.Identity<decimal?>(0, 1, 1, 2, 3, 5, 8, 13, 21, 34);
        using var indicator = Indicator.Change(identity);

        // assert
        Assert.Collection(indicator,
            x => Assert.Null(x),
            x => Assert.Equal(1, x),
            x => Assert.Equal(0, x),
            x => Assert.Equal(1, x),
            x => Assert.Equal(1, x),
            x => Assert.Equal(2, x),
            x => Assert.Equal(3, x),
            x => Assert.Equal(5, x),
            x => Assert.Equal(8, x),
            x => Assert.Equal(13, x));
    }
}