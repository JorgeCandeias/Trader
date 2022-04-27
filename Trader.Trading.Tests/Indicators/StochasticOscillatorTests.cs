using Outcompute.Trader.Core.Mathematics;
using Outcompute.Trader.Indicators;
using Outcompute.Trader.Trading.Indicators;

namespace Outcompute.Trader.Trading.Tests.Indicators;

public class StochasticOscillatorTests
{
    private static StochasticOscillatorResult Round(StochasticOscillatorResult value, int decimals = 2)
    {
        return new StochasticOscillatorResult(MathN.Round(value.K, decimals), MathN.Round(value.D, decimals));
    }

    [Fact]
    public void YieldsOutput()
    {
        // arrange
        var data = TestData.BtcBusdHistoricalData.Take(10).ToHLC().ToList();

        // act
        using var indicator = data.Identity().StochasticOscillator(3, 1, 3);

        // assert
        Assert.Collection(indicator,
            x => Assert.Equal(StochasticOscillatorResult.Empty, x),
            x => Assert.Equal(StochasticOscillatorResult.Empty, x),
            x => Assert.Equal(new StochasticOscillatorResult(35.70M, null), Round(x, 2)),
            x => Assert.Equal(new StochasticOscillatorResult(37.92M, null), Round(x, 2)),
            x => Assert.Equal(new StochasticOscillatorResult(16.92M, 30.18M), Round(x, 2)),
            x => Assert.Equal(new StochasticOscillatorResult(81.21M, 45.35M), Round(x, 2)),
            x => Assert.Equal(new StochasticOscillatorResult(80.57M, 59.56M), Round(x, 2)),
            x => Assert.Equal(new StochasticOscillatorResult(78.51M, 80.10M), Round(x, 2)),
            x => Assert.Equal(new StochasticOscillatorResult(64.60M, 74.56M), Round(x, 2)),
            x => Assert.Equal(new StochasticOscillatorResult(79.79M, 74.30M), Round(x, 2)));
    }
}