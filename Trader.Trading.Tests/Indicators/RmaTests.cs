using Outcompute.Trader.Core.Mathematics;
using Outcompute.Trader.Trading.Indicators;

namespace Outcompute.Trader.Trading.Tests.Indicators;

public class RmaTests
{
    [Fact]
    public void CalculatesRma()
    {
        // arrange
        var data = TestData.BtcBusdHistoricalData.Take(10).Select(x => x.Close).ToList();

        // act
        using var indicator = data.Identity().Rma(3);

        // assert
        Assert.Collection(indicator,
            x => Assert.Null(x),
            x => Assert.Null(x),
            x => Assert.Equal(10141.62M, MathN.Round(x, 2)),
            x => Assert.Equal(10097.92M, MathN.Round(x, 2)),
            x => Assert.Equal(9967.59M, MathN.Round(x, 2)),
            x => Assert.Equal(9478.31M, MathN.Round(x, 2)),
            x => Assert.Equal(9131.12M, MathN.Round(x, 2)),
            x => Assert.Equal(8776.68M, MathN.Round(x, 2)),
            x => Assert.Equal(8580.17M, MathN.Round(x, 2)),
            x => Assert.Equal(8455.57M, MathN.Round(x, 2)));
    }
}