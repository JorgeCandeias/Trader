using Outcompute.Trader.Core.Mathematics;
using Outcompute.Trader.Trading.Indicators;

namespace Outcompute.Trader.Trading.Tests.Indicators;

public class WmaTests
{
    [Fact]
    public void YieldsWma()
    {
        // act
        using var indicator = new Wma(3)
        {
            10254.92M, 10171.06M, 9998.87M, 10010.53M, 9706.93M, 8499.75M, 8436.75M, 8067.78M, 8187.15M, 8206.39M
        };

        // assert
        Assert.Collection(indicator,
            x => Assert.Null(x),
            x => Assert.Null(x),
            x => Assert.Equal(10098.94M, MathN.Round(x, 2)),
            x => Assert.Equal(10033.40M, MathN.Round(x, 2)),
            x => Assert.Equal(9856.79M, MathN.Round(x, 2)),
            x => Assert.Equal(9153.94M, MathN.Round(x, 2)),
            x => Assert.Equal(8669.45M, MathN.Round(x, 2)),
            x => Assert.Equal(8262.76M, MathN.Round(x, 2)),
            x => Assert.Equal(8188.96M, MathN.Round(x, 2)),
            x => Assert.Equal(8176.88M, MathN.Round(x, 2)));
    }
}