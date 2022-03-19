using Outcompute.Trader.Core.Mathematics;
using Outcompute.Trader.Trading.Indicators;

namespace Outcompute.Trader.Trading.Tests.Indicators;

public class WmaTests
{
    [Fact]
    public void YieldsWma()
    {
        // arrange
        var data = TestData.BtcBusdHistoricalData.Take(10).Select(x => x.Close).ToList();

        // act
        using var indicator = data.Identity().Wma(3);

        // assert
        Assert.Collection(indicator,
            x => Assert.Null(x),
            x => Assert.Null(x),
            x => Assert.Equal(10098.94M, MathN.Round(x, 2)),
            x => Assert.Equal(10033.40M, MathN.Round(x, 2)),
            x => Assert.Equal(9856.79M, MathN.Round(x, 2)),
            x => Assert.Equal(9153.94M, MathN.Round(x, 2)),
            x => Assert.Equal(8669.45M, MathN.Round(x, 2)),
            x => Assert.Equal(8262.76M, MathN.Round(x, 2)),
            x => Assert.Equal(8188.96M, MathN.Round(x, 2)),
            x => Assert.Equal(8176.88M, MathN.Round(x, 2)));
    }
}