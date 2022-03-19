using Outcompute.Trader.Core.Mathematics;
using Outcompute.Trader.Trading.Indicators;

namespace Outcompute.Trader.Trading.Tests.Indicators;

public class CciTests
{
    [Fact]
    public void YieldsCci()
    {
        // arrange
        var data = TestData.BtcBusdHistoricalData.Take(10).ToHLC3().ToList();

        // act
        using var indicator = data.Identity().Cci(3);

        // assert
        Assert.Collection(indicator,
            x => Assert.Null(x),
            x => Assert.Null(x),
            x => Assert.Equal(-100M, MathN.Round(x, 2)),
            x => Assert.Equal(-78.20M, MathN.Round(x, 2)),
            x => Assert.Equal(-100M, MathN.Round(x, 2)),
            x => Assert.Equal(-100M, MathN.Round(x, 2)),
            x => Assert.Equal(9.19M, MathN.Round(x, 2)),
            x => Assert.Equal(22.17M, MathN.Round(x, 2)),
            x => Assert.Equal(-19.01M, MathN.Round(x, 2)),
            x => Assert.Equal(76.59M, MathN.Round(x, 2)));
    }
}