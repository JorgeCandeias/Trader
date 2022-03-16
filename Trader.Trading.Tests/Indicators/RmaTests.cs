using Outcompute.Trader.Core.Mathematics;
using Outcompute.Trader.Trading.Indicators;

namespace Outcompute.Trader.Trading.Tests.Indicators;

public class RmaTests
{
    [Fact]
    public void CalculatesRma()
    {
        // act
        using var indicator = new Rma(3)
        {
            10254.92M, 10171.06M, 9998.87M, 10010.53M, 9706.93M, 8499.75M, 8436.75M, 8067.78M, 8187.15M, 8206.39M
        };

        // assert
        Assert.Collection(indicator,
            x => Assert.Null(x),
            x => Assert.Null(x),
            x => Assert.Equal(10141.62M, MathN.Round(x, 2)),
            x => Assert.Equal(10097.92M, MathN.Round(x, 2)),
            x => Assert.Equal(9967.59M, MathN.Round(x, 2)),
            x => Assert.Equal(9478.31M, MathN.Round(x, 2)),
            x => Assert.Equal(9131.12M, MathN.Round(x, 2)),
            x => Assert.Equal(8776.68M, MathN.Round(x, 2)),
            x => Assert.Equal(8580.17M, MathN.Round(x, 2)),
            x => Assert.Equal(8455.57M, MathN.Round(x, 2)));
    }
}