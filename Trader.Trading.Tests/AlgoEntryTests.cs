using Outcompute.Trader.Trading.Configuration;

namespace Outcompute.Trader.Trading.Tests;

public class AlgoEntryTests
{
    [Fact]
    public void GetsProperties()
    {
        // arrange
        var name = "Algo1";
        var entry = new AlgoEntry(name);

        // assert
        Assert.Equal(name, entry.Name);
    }
}