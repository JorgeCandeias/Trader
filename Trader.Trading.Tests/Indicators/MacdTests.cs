using Outcompute.Trader.Core.Mathematics;
using Outcompute.Trader.Indicators;

namespace Outcompute.Trader.Trading.Tests.Indicators;

public class MacdTests
{
    public static MacdResult Round(MacdResult value, int decimals = 2)
    {
        return new MacdResult(
            MathN.Round(value.Macd, decimals),
            MathN.Round(value.Signal, decimals),
            MathN.Round(value.Histogram, decimals));
    }

    [Fact]
    public void CalculatesBB()
    {
        // arrange
        var data = TestData.BtcBusdHistoricalData.Take(10).Select(x => x.Close).ToList();

        // act
        using var indicator = data.Identity().Macd(3, 4, 5);

        // assert
        Assert.Collection(indicator,
            x => Assert.Equal(MacdResult.Empty, x),
            x => Assert.Equal(MacdResult.Empty, x),
            x => Assert.Equal(MacdResult.Empty, x),
            x => Assert.Equal(new MacdResult(-32.77M, null, null), Round(x)),
            x => Assert.Equal(new MacdResult(-56.58M, null, null), Round(x)),
            x => Assert.Equal(new MacdResult(-173.12M, null, null), Round(x)),
            x => Assert.Equal(new MacdResult(-179.76M, null, null), Round(x)),
            x => Assert.Equal(new MacdResult(-182.70M, -124.99M, -57.71M), Round(x)),
            x => Assert.Equal(new MacdResult(-135.10M, -128.36M, -6.74M), Round(x)),
            x => Assert.Equal(new MacdResult(-91.88M, -116.20M, 24.32M), Round(x)));
    }
}