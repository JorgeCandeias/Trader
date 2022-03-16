using Outcompute.Trader.Core.Mathematics;
using Outcompute.Trader.Trading.Indicators;

namespace Outcompute.Trader.Trading.Tests.Indicators;

public class HmaTests
{
    [Fact]
    public void YieldsHma()
    {
        // act
        using var indicator = new Hma(3)
        {
            10254.92M, 10171.06M, 9998.87M, 10010.53M, 9706.93M, 8499.75M, 8436.75M, 8067.78M, 8187.15M, 8206.39M
        };

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