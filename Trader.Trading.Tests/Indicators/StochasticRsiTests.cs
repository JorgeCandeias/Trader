using Outcompute.Trader.Core.Mathematics;
using Outcompute.Trader.Trading.Indicators;

namespace Outcompute.Trader.Trading.Tests.Indicators;

public class StochasticRsiTests
{
    private static StochasticRsiResult Round(StochasticRsiResult value, int decimals = 2)
    {
        return new StochasticRsiResult(MathN.Round(value.K, decimals), MathN.Round(value.D, decimals));
    }

    [Fact]
    public void YieldsOutput()
    {
        // arrange
        var data = TestData.BtcBusdHistoricalData.Take(20).Select(x => x.Close).ToList();

        // act
        using var indicator = data.Identity().StochasticRsi(3, 3, 3, 3);

        // assert
        Assert.Collection(indicator,
            x => Assert.Equal(StochasticRsiResult.Empty, x),
            x => Assert.Equal(StochasticRsiResult.Empty, x),
            x => Assert.Equal(StochasticRsiResult.Empty, x),
            x => Assert.Equal(StochasticRsiResult.Empty, x),
            x => Assert.Equal(StochasticRsiResult.Empty, x),
            x => Assert.Equal(StochasticRsiResult.Empty, x),
            x => Assert.Equal(new StochasticRsiResult(0M, null), Round(x)),
            x => Assert.Equal(new StochasticRsiResult(0M, null), Round(x)),
            x => Assert.Equal(new StochasticRsiResult(33.33M, 11.11M), Round(x)),
            x => Assert.Equal(new StochasticRsiResult(66.67M, 33.33M), Round(x)),
            x => Assert.Equal(new StochasticRsiResult(66.67M, 55.56M), Round(x)),
            x => Assert.Equal(new StochasticRsiResult(66.67M, 66.67M), Round(x)),
            x => Assert.Equal(new StochasticRsiResult(66.67M, 66.67M), Round(x)),
            x => Assert.Equal(new StochasticRsiResult(100M, 77.78M), Round(x)),
            x => Assert.Equal(new StochasticRsiResult(66.67M, 77.78M), Round(x)),
            x => Assert.Equal(new StochasticRsiResult(33.33M, 66.67M), Round(x)),
            x => Assert.Equal(new StochasticRsiResult(0M, 33.33M), Round(x)),
            x => Assert.Equal(new StochasticRsiResult(0M, 11.11M), Round(x)),
            x => Assert.Equal(new StochasticRsiResult(33.33M, 11.11M), Round(x)),
            x => Assert.Equal(new StochasticRsiResult(62.67M, 32.00M), Round(x)));
    }
}