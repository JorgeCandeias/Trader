using Outcompute.Trader.Core.Mathematics;
using Outcompute.Trader.Indicators;

namespace Outcompute.Trader.Trading.Tests.Indicators;

public class UltimateOscillatorTests
{
    [Fact]
    public void YieldsOutput()
    {
        // arrange
        var data = TestData.BtcBusdHistoricalData.Take(10).ToHLC().ToList();

        // act
        using var indicator = data.Identity().UltimateOscillator(1, 2, 3);

        // assert
        Assert.Collection(indicator,
            x => Assert.Null(x),
            x => Assert.Null(x),
            x => Assert.Equal(30.39M, MathN.Round(x, 2)),
            x => Assert.Equal(65.00M, MathN.Round(x, 2)),
            x => Assert.Equal(28.59M, MathN.Round(x, 2)),
            x => Assert.Equal(82.58M, MathN.Round(x, 2)),
            x => Assert.Equal(58.52M, MathN.Round(x, 2)),
            x => Assert.Equal(68.19M, MathN.Round(x, 2)),
            x => Assert.Equal(77.15M, MathN.Round(x, 2)),
            x => Assert.Equal(67.00M, MathN.Round(x, 2)));
    }
}