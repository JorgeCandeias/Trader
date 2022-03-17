using Outcompute.Trader.Trading.Indicators;

namespace Outcompute.Trader.Trading.Tests.Indicators;

public class MovingWindowTests
{
    [Fact]
    public void YieldsMovingWindow()
    {
        // arrange
        var data = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        // act
        using var indicator = new MovingWindow<int>(3);
        indicator.AddRange(data);

        // assert
        Assert.Collection(indicator,
            x => Assert.Equal(1, x.Sum()),
            x => Assert.Equal(3, x.Sum()),
            x => Assert.Equal(6, x.Sum()),
            x => Assert.Equal(9, x.Sum()),
            x => Assert.Equal(12, x.Sum()),
            x => Assert.Equal(15, x.Sum()),
            x => Assert.Equal(18, x.Sum()),
            x => Assert.Equal(21, x.Sum()),
            x => Assert.Equal(24, x.Sum()),
            x => Assert.Equal(27, x.Sum()));
    }
}