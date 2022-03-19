using Outcompute.Trader.Core.Mathematics;
using Outcompute.Trader.Trading.Indicators;

namespace Outcompute.Trader.Trading.Tests.Indicators;

public class RsiTests
{
    [Fact]
    public void YieldsOutput()
    {
        // arrange
        var data = TestData.BtcBusdHistoricalData.Take(10).Select(x => x.Close).ToList();

        // act
        using var indicator = data.Identity().Rsi(3);

        // assert
        Assert.Collection(indicator,
            x => Assert.Null(x),
            x => Assert.Null(x),
            x => Assert.Null(x),
            x => Assert.Equal(4.36M, MathN.Round(x, 2)),
            x => Assert.Equal(1.61M, MathN.Round(x, 2)),
            x => Assert.Equal(0.34M, MathN.Round(x, 2)),
            x => Assert.Equal(0.32M, MathN.Round(x, 2)),
            x => Assert.Equal(0.21M, MathN.Round(x, 2)),
            x => Assert.Equal(14.29M, MathN.Round(x, 2)),
            x => Assert.Equal(17.11M, MathN.Round(x, 2)));
    }
}