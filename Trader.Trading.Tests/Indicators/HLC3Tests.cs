using Outcompute.Trader.Core.Mathematics;
using Outcompute.Trader.Trading.Indicators;

namespace Outcompute.Trader.Trading.Tests.Indicators;

public static class HLC3Tests
{
    [Fact]
    public static void YieldsOutput()
    {
        // arrange
        using var indicator = new HLC3()
        {
            new HLC(10, 20, 30),
            new HLC(10, 20, 20),
            new HLC(20, 20, 30)
        };

        // assert
        Assert.Collection(indicator,
            x => Assert.Equal(20, x),
            x => Assert.Equal(16.67M, MathN.Round(x, 2)),
            x => Assert.Equal(23.33M, MathN.Round(x, 2)));
    }
}