using Outcompute.Trader.Core.Mathematics;
using Outcompute.Trader.Indicators;

namespace Outcompute.Trader.Trading.Tests.Indicators;

public class HmaTests
{
    [Fact]
    public void YieldsHma()
    {
        // arrange
        var data = TestData.BtcBusdHistoricalData.Take(10).Select(x => x.Close).ToList();

        // act
        using var indicator = data.Identity().Hma(3);

        // assert
        Assert.Collection(indicator,
            x => Assert.Null(x),
            x => Assert.Null(x),
            x => Assert.Equal(9898.80M, MathN.Round(x, 2)),
            x => Assert.Equal(9987.66M, MathN.Round(x, 2)),
            x => Assert.Equal(9557.07M, MathN.Round(x, 2)),
            x => Assert.Equal(7845.56M, MathN.Round(x, 2)),
            x => Assert.Equal(8204.05M, MathN.Round(x, 2)),
            x => Assert.Equal(7872.80M, MathN.Round(x, 2)),
            x => Assert.Equal(8185.34M, MathN.Round(x, 2)),
            x => Assert.Equal(8235.90M, MathN.Round(x, 2)));
    }
}