using Outcompute.Trader.Core.Mathematics;
using Outcompute.Trader.Indicators;

namespace Outcompute.Trader.Trading.Tests.Indicators;

public class VwmaTests
{
    [Fact]
    public void YieldsVwma()
    {
        // arrange
        var data = TestData.BtcBusdHistoricalData.Take(10).ToCV().ToList();

        // act
        using var indicator = data.Identity().Vwma(3);

        // assert
        Assert.Collection(indicator,
            x => Assert.Null(x),
            x => Assert.Null(x),
            x => Assert.Equal(10131.35M, MathN.Round(x, 2)),
            x => Assert.Equal(10101.86M, MathN.Round(x, 2)),
            x => Assert.Equal(9750.48M, MathN.Round(x, 2)),
            x => Assert.Equal(8886.18M, MathN.Round(x, 2)),
            x => Assert.Equal(8760.14M, MathN.Round(x, 2)),
            x => Assert.Equal(8295.27M, MathN.Round(x, 2)),
            x => Assert.Equal(8173.82M, MathN.Round(x, 2)),
            x => Assert.Equal(8115.47M, MathN.Round(x, 2)));
    }
}