using Outcompute.Trader.Core.Mathematics;
using Outcompute.Trader.Indicators;
using Outcompute.Trader.Trading.Indicators;

namespace Outcompute.Trader.Trading.Tests.Indicators;

public class BullBearPowerTests
{
    public static BBP Round(BBP value, int decimals = 0)
    {
        return new BBP(MathN.Round(value.BullPower, decimals), MathN.Round(value.BearPower, decimals), MathN.Round(value.Power, decimals));
    }

    [Fact]
    public void CalculatesBBP()
    {
        // arrange
        var data = TestData.BtcBusdHistoricalData.Take(10).ToHLC().ToList();

        // act
        using var indicator = data.Identity().BullBearPower(3);

        // assert
        Assert.Collection(indicator,
            x => Assert.Equal(BBP.Empty, x),
            x => Assert.Equal(BBP.Empty, x),
            x => Assert.Equal(new BBP(27.82M, -201.35M, -173.52M), Round(x, 2)),
            x => Assert.Equal(new BBP(-25.95M, -201.77M, -227.73M), Round(x, 2)),
            x => Assert.Equal(new BBP(146.20M, -278.75M, -132.55M), Round(x, 2)),
            x => Assert.Equal(new BBP(587.47M, -7395.63M, -6808.15M), Round(x, 2)),
            x => Assert.Equal(new BBP(-88.15M, -590.61M, -678.76M), Round(x, 2)),
            x => Assert.Equal(new BBP(19.33M, -1241.98M, -1222.66M), Round(x, 2)),
            x => Assert.Equal(new BBP(-57.32M, -461.04M, -518.35M), Round(x, 2)),
            x => Assert.Equal(new BBP(59.38M, -243.73M, -184.35M), Round(x, 2)));
    }
}