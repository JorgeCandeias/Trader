using Outcompute.Trader.Core.Mathematics;
using Outcompute.Trader.Trading.Indicators;

namespace Outcompute.Trader.Trading.Tests.Indicators;

public class VarianceTests
{
    [Fact]
    public void CalculatesVariance()
    {
        // arrange
        var data = TestData.BtcBusdHistoricalData.Take(10).Select(x => x.Close).ToList();

        // act
        using var indicator = new Variance(3);
        indicator.AddRange(data);

        // assert
        Assert.Collection(indicator,
            x => Assert.Null(x),
            x => Assert.Null(x),
            x => Assert.Equal(11360.39M, MathN.Round(x, 2)),
            x => Assert.Equal(6172.80M, MathN.Round(x, 2)),
            x => Assert.Equal(19726.43M, MathN.Round(x, 2)),
            x => Assert.Equal(425768.08M, MathN.Round(x, 2)),
            x => Assert.Equal(341623.31M, MathN.Round(x, 2)),
            x => Assert.Equal(36300.66M, MathN.Round(x, 2)),
            x => Assert.Equal(23632.02M, MathN.Round(x, 2)),
            x => Assert.Equal(3759.12M, MathN.Round(x, 2)));
    }
}