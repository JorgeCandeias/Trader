using Outcompute.Trader.Core.Mathematics;
using Outcompute.Trader.Indicators;

namespace Outcompute.Trader.Trading.Tests.Indicators;

public class IchimokuCloudTests
{
    public static IchimokuCloudResult Round(IchimokuCloudResult value, int decimals = 2)
    {
        return new IchimokuCloudResult(MathN.Round(value.ConversionLine, decimals), MathN.Round(value.BaseLine, decimals), MathN.Round(value.LeadLine1, decimals), MathN.Round(value.LeadLine2, decimals));
    }

    [Fact]
    public void CalculatesIM()
    {
        // arrange
        var data = TestData.BtcBusdHistoricalData.Take(10).ToHL().ToList();

        // act
        using var indicator = data.Identity().IchimokuCloud(3, 4, 5, 6);

        // assert
        Assert.Collection(indicator,
            x => Assert.Equal(IchimokuCloudResult.Empty, x),
            x => Assert.Equal(IchimokuCloudResult.Empty, x),
            x => Assert.Equal(new IchimokuCloudResult(10067.08M, null, null, null), Round(x, 2)),
            x => Assert.Equal(new IchimokuCloudResult(10053.92M, 10067.08M, 10060.50M, null), Round(x, 2)),
            x => Assert.Equal(new IchimokuCloudResult(9891.10M, 9923.14M, 9907.12M, 9959.16M), Round(x, 2)),
            x => Assert.Equal(new IchimokuCloudResult(5925.06M, 5984.72M, 5954.89M, 6016.76M), Round(x, 2)),
            x => Assert.Equal(new IchimokuCloudResult(5918.85M, 5925.06M, 5921.96M, 5984.72M), Round(x, 2)),
            x => Assert.Equal(new IchimokuCloudResult(5791.55M, 5918.85M, 5855.20M, 5925.06M), Round(x, 2)),
            x => Assert.Equal(new IchimokuCloudResult(7964.02M, 5791.55M, 6877.78M, 5918.85M), Round(x, 2)),
            x => Assert.Equal(new IchimokuCloudResult(7830.66M, 7964.02M, 7897.34M, 5791.55M), Round(x, 2)));
    }
}