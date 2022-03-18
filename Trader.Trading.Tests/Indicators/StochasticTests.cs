using Outcompute.Trader.Core.Mathematics;
using Outcompute.Trader.Trading.Indicators;

namespace Outcompute.Trader.Trading.Tests.Indicators;

public class StochasticTests
{
    [Fact]
    public void YieldsOutput()
    {
        // arrange
        var data = TestData.BtcBusdHistoricalData.Take(10).ToHLC().ToList();

        // act
        using var indicator = Indicator.Stochastic(3);
        indicator.AddRange(data);

        // assert
        Assert.Collection(indicator,
            x => Assert.Null(x),
            x => Assert.Null(x),
            x => Assert.Equal(35.70M, MathN.Round(x, 2)),
            x => Assert.Equal(37.92M, MathN.Round(x, 2)),
            x => Assert.Equal(16.92M, MathN.Round(x, 2)),
            x => Assert.Equal(81.21M, MathN.Round(x, 2)),
            x => Assert.Equal(80.57M, MathN.Round(x, 2)),
            x => Assert.Equal(78.51M, MathN.Round(x, 2)),
            x => Assert.Equal(64.60M, MathN.Round(x, 2)),
            x => Assert.Equal(79.79M, MathN.Round(x, 2)));
    }
}