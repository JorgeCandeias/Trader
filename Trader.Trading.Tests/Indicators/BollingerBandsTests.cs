using Outcompute.Trader.Core.Mathematics;
using Outcompute.Trader.Indicators;

namespace Outcompute.Trader.Trading.Tests.Indicators;

public class BollingerBandsTests
{
    public static BollingerBand Round(BollingerBand value, int decimals = 0)
    {
        return new BollingerBand(MathN.Round(value.Average, decimals), MathN.Round(value.High, decimals), MathN.Round(value.Low, decimals));
    }

    [Fact]
    public void CalculatesBB()
    {
        // arrange
        var data = TestData.BtcBusdHistoricalData.Take(10).Select(x => x.Close).ToList();

        // act
        using var indicator = data.Identity().BollingerBands(3, 2);

        // assert
        Assert.Collection(indicator,
            x => Assert.Equal(BollingerBand.Empty, x),
            x => Assert.Equal(BollingerBand.Empty, x),
            x => Assert.Equal(new BollingerBand(10141.62M, 10354.79M, 9928.45M), Round(x, 2)),
            x => Assert.Equal(new BollingerBand(10060.15M, 10217.29M, 9903.02M), Round(x, 2)),
            x => Assert.Equal(new BollingerBand(9905.44M, 10186.34M, 9624.54M), Round(x, 2)),
            x => Assert.Equal(new BollingerBand(9405.74M, 10710.75M, 8100.72M), Round(x, 2)),
            x => Assert.Equal(new BollingerBand(8881.14M, 10050.11M, 7712.17M), Round(x, 2)),
            x => Assert.Equal(new BollingerBand(8334.76M, 8715.81M, 7953.71M), Round(x, 2)),
            x => Assert.Equal(new BollingerBand(8230.56M, 8538.01M, 7923.11M), Round(x, 2)),
            x => Assert.Equal(new BollingerBand(8153.77M, 8276.40M, 8031.15M), Round(x, 2)));
    }
}