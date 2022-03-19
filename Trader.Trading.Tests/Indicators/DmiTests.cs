using Outcompute.Trader.Core.Mathematics;
using Outcompute.Trader.Trading.Indicators;

namespace Outcompute.Trader.Trading.Tests.Indicators;

public class DmiTests
{
    public static DMI Round(DMI value, int decimals = 0)
    {
        return new DMI(MathN.Round(value.Plus, decimals), MathN.Round(value.Minus, decimals), MathN.Round(value.Adx, decimals));
    }

    [Fact]
    public void CalculatesDmi()
    {
        // arrange
        var data = TestData.BtcBusdHistoricalData.Take(10).ToHLC().ToList();

        // act
        using var indicator = data.Identity().Dmi(3, 3);

        // assert
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
    }
}