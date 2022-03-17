using Outcompute.Trader.Core.Mathematics;
using Outcompute.Trader.Trading.Indicators;

namespace Outcompute.Trader.Trading.Tests.Indicators;

public class StDevTests
{
    [Fact]
    public void CalculatesStDev()
    {
        // arrange
        var data = TestData.BtcBusdHistoricalData.Take(10).Select(x => x.Close).ToList();

        // act
        using var indicator = new StDev(3);
        indicator.AddRange(data);

        // assert
        Assert.Collection(indicator,
            x => Assert.Null(x),
            x => Assert.Null(x),
            x => Assert.Equal(106.59M, MathN.Round(x, 2)),
            x => Assert.Equal(78.57M, MathN.Round(x, 2)),
            x => Assert.Equal(140.45M, MathN.Round(x, 2)),
            x => Assert.Equal(652.51M, MathN.Round(x, 2)),
            x => Assert.Equal(584.49M, MathN.Round(x, 2)),
            x => Assert.Equal(190.53M, MathN.Round(x, 2)),
            x => Assert.Equal(153.73M, MathN.Round(x, 2)),
            x => Assert.Equal(61.31M, MathN.Round(x, 2)));
    }
}