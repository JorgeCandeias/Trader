using Outcompute.Trader.Core.Mathematics;
using Outcompute.Trader.Trading.Indicators;

namespace Outcompute.Trader.Trading.Tests.Indicators;

public class AwesomeOscillatorTests
{
    [Fact]
    public void CalculatesAwesomeOscillator()
    {
        var data = TestData.BtcBusdHistoricalData.Take(10).ToHL().ToList();

        // act
        using var indicator = new AwesomeOscillator(3, 5);
        indicator.AddRange(data);

        // assert
        Assert.Collection(indicator,
            x => Assert.Null(x),
            x => Assert.Null(x),
            x => Assert.Null(x),
            x => Assert.Null(x),
            x => Assert.Equal(-65.69M, MathN.Round(x, 2)),
            x => Assert.Equal(-631.68M, MathN.Round(x, 2)),
            x => Assert.Equal(-790.94M, MathN.Round(x, 2)),
            x => Assert.Equal(-1010.95M, MathN.Round(x, 2)),
            x => Assert.Equal(125.03M, MathN.Round(x, 2)),
            x => Assert.Equal(353.57M, MathN.Round(x, 2)));
    }
}