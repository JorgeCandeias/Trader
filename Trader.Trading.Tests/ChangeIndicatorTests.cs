using Outcompute.Trader.Trading.Indicators;

namespace Outcompute.Trader.Trading.Tests;

public class ChangeIndicatorTests
{
    [Fact]
    public void YieldsChange()
    {
        // act
        var indicator = new ChangeIndicator
        {
            0, 1, 1, 2, 3, 5, 8, 13, 21, 34
        };

        // assert
        Assert.True(indicator.SequenceEqual(new decimal?[] { null, 1, 0, 1, 1, 2, 3, 5, 8, 13 }));
    }
}