using Outcompute.Trader.Indicators;

namespace Outcompute.Trader.Trading.Tests.Indicators;

public class TechnicalRatingsTests
{
    [Fact]
    public void CalculatesRatings()
    {
        // arrange
        var data = TestData.BtcBusdHistoricalData.Take(30).ToList();

        // act
        using var indicator = data.Identity().TechnicalRatings();

        // assert
        //Assert.Collection(indicator);
    }
}