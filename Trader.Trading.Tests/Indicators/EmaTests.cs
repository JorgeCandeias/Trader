using Outcompute.Trader.Core.Mathematics;
using Outcompute.Trader.Trading.Indicators;

namespace Outcompute.Trader.Trading.Tests.Indicators;

public class EmaTests
{
    [Fact]
    public void YieldsEma()
    {
        // act
        using var indicator = new Ema(3)
        {
            10254.92M, 10171.06M, 9998.87M, 10010.53M, 9706.93M, 8499.75M, 8436.75M, 8067.78M, 8187.15M, 8206.39M
        };

        // assert
        Assert.Collection(indicator,
            x => Assert.Null(x),
            x => Assert.Null(x),
            x => Assert.Equal(10141.62M, MathN.Round(x, 2)),
            x => Assert.Equal(10076.07M, MathN.Round(x, 2)),
            x => Assert.Equal(9891.50M, MathN.Round(x, 2)),
            x => Assert.Equal(9195.63M, MathN.Round(x, 2)),
            x => Assert.Equal(8816.19M, MathN.Round(x, 2)),
            x => Assert.Equal(8441.98M, MathN.Round(x, 2)),
            x => Assert.Equal(8314.57M, MathN.Round(x, 2)),
            x => Assert.Equal(8260.48M, MathN.Round(x, 2)));
    }
}