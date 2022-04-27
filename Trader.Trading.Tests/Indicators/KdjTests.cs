using Outcompute.Trader.Core.Mathematics;
using Outcompute.Trader.Indicators;
using Outcompute.Trader.Trading.Indicators;

namespace Outcompute.Trader.Trading.Tests.Indicators;

public class KdjTests
{
    public static KdjValue Round(KdjValue value, int decimals = 2)
    {
        return new KdjValue
        {
            Price = MathN.Round(value.Price, decimals),
            K = MathN.Round(value.K, decimals),
            D = MathN.Round(value.D, decimals),
            J = MathN.Round(value.J, decimals),
            Cross = value.Cross
        };
    }

    [Fact]
    public void CalculatesKdj()
    {
        // arrange
        var data = TestData.BtcBusdHistoricalData.Take(10).ToHLC().ToList();

        // act
        using var indicator = data.Identity().Kdj(9, 3, 3);

        // assert
        /*
        Assert.Collection(indicator,
            x => Assert.Equal(DMI.Empty, x),
            x => Assert.Equal(DMI.Empty, x),
            x => Assert.Equal(DMI.Empty, x),
            x => Assert.Equal(new DMI(0M, 35.12M, null), Round(x, 2)),
            x => Assert.Equal(new DMI(0M, 48.93M, null), Round(x, 2)),
            x => Assert.Equal(new DMI(0M, 94.75M, 100M), Round(x, 2)),
            x => Assert.Equal(new DMI(0M, 87.06M, 100M), Round(x, 2)),
            x => Assert.Equal(new DMI(0M, 85.71M, 100M), Round(x, 2)),
            x => Assert.Equal(new DMI(0M, 77.05M, 100M), Round(x, 2)),
            x => Assert.Equal(new DMI(2.11M, 69.17M, 98.03M), Round(x, 2)));
        */
    }
}