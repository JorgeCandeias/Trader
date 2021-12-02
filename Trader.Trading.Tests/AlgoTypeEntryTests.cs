using Outcompute.Trader.Trading.Configuration;

namespace Outcompute.Trader.Trading.Tests;

public class AlgoTypeEntryTests
{
    [Fact]
    public void GetsProperties()
    {
        // arrange
        var name = "Algo1";
        var type = typeof(AlgoTypeEntryTests);

        // act
        var entry = new AlgoTypeEntry(name, type);

        // assert
        Assert.Equal(name, entry.Name);
        Assert.Equal(type, entry.Type);
    }
}