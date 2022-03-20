using Outcompute.Trader.Core.Mathematics;
using Outcompute.Trader.Trading.Indicators;

namespace Outcompute.Trader.Trading.Tests.Indicators;

public class WilliamsPercentRangeTests
{
    [Fact]
    public void YieldsOutput()
    {
        // arrange
        var data = TestData.BtcBusdHistoricalData.Take(10).ToHLC().ToList();

        // act
        using var indicator = data.Identity().WilliamsPercentRange(3);

        // assert
        Assert.Collection(indicator,
            x => Assert.Null(x),
            x => Assert.Null(x),
            x => Assert.Equal(-64.30M, MathN.Round(x, 2)),
            x => Assert.Equal(-62.08M, MathN.Round(x, 2)),
            x => Assert.Equal(-83.08M, MathN.Round(x, 2)),
            x => Assert.Equal(-18.79M, MathN.Round(x, 2)),
            x => Assert.Equal(-19.43M, MathN.Round(x, 2)),
            x => Assert.Equal(-21.49M, MathN.Round(x, 2)),
            x => Assert.Equal(-35.40M, MathN.Round(x, 2)),
            x => Assert.Equal(-20.21M, MathN.Round(x, 2)));
    }
}