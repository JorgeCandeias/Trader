using Outcompute.Trader.Core.Mathematics;
using Outcompute.Trader.Trading.Indicators;

namespace Outcompute.Trader.Trading.Tests.Indicators;

public class AtrTests
{
    [Fact]
    public void CalculatesAtr()
    {
        // BTCBUSD Historical 2019-09-19 / 2019-09-28
        var history = new OHLCV[]
        {
            new OHLCV(9881.43M, 10305.58M, 9828.59M, 10254.92M, 1.0510M),
            new OHLCV(10214.31M, 10233.53M, 10078.91M, 10171.06M, 6.9162M),
            new OHLCV(10169.44M, 10169.44M, 9940.27M, 9998.87M, 3.0530M),
            new OHLCV(9928.07M, 10050.12M, 9874.30M, 10010.53M, 1.7977M),
            new OHLCV(10037.70M, 10037.70M, 9612.75M, 9706.93M, 28.15M),
            new OHLCV(9675.42M, 9783.10M, 1800.00M, 8499.75M, 65.02M),
            new OHLCV(8558.92M, 8728.04M, 8225.58M, 8436.75M, 30.06M),
            new OHLCV(8415.14M, 8461.31M, 7200.00M, 8067.78M, 77.14M),
            new OHLCV(8067.78M, 8257.25M, 7853.53M, 8187.15M, 20.75M),
            new OHLCV(8187.15M, 8319.86M, 8016.75M, 8206.39M, 24.10M)
        };

        // act
        using var indicator = history.ToHLC().Identity().Atr(3, AtrMethod.Rma);

        // assert
        Assert.Collection(indicator,
            x => Assert.Null(x),
            x => Assert.Null(x),
            x => Assert.Equal(294.60M, MathN.Round(x, 2)),
            x => Assert.Equal(255.00M, MathN.Round(x, 2)),
            x => Assert.Equal(311.65M, MathN.Round(x, 2)),
            x => Assert.Equal(2868.80M, MathN.Round(x, 2)),
            x => Assert.Equal(2080.02M, MathN.Round(x, 2)),
            x => Assert.Equal(1807.12M, MathN.Round(x, 2)),
            x => Assert.Equal(1339.32M, MathN.Round(x, 2)),
            x => Assert.Equal(993.92M, MathN.Round(x, 2)));
    }
}