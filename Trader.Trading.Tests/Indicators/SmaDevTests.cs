using Outcompute.Trader.Core.Mathematics;
using Outcompute.Trader.Trading.Indicators;

namespace Outcompute.Trader.Trading.Tests.Indicators;

public class SmaDevTests
{
    [Fact]
    public void YieldsSmaDev()
    {
        // arrange
        var data = TestData.BtcBusdHistoricalData.Take(10).Select(x => x.Close).ToList();

        // act
        using var indicator = data.Identity().SmaDev(3);

        // assert
        Assert.Collection(indicator,
            x => Assert.Null(x),
            x => Assert.Null(x),
            x => Assert.Equal(95.16M, MathN.Round(x, 2)),
            x => Assert.Equal(73.94M, MathN.Round(x, 2)),
            x => Assert.Equal(132.34M, MathN.Round(x, 2)),
            x => Assert.Equal(603.99M, MathN.Round(x, 2)),
            x => Assert.Equal(550.52M, MathN.Round(x, 2)),
            x => Assert.Equal(177.99M, MathN.Round(x, 2)),
            x => Assert.Equal(137.46M, MathN.Round(x, 2)),
            x => Assert.Equal(57.33M, MathN.Round(x, 2)));
    }
}