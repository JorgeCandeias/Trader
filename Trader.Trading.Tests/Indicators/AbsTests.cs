using Outcompute.Trader.Trading.Indicators;

namespace Outcompute.Trader.Trading.Tests.Indicators;

public static class AbsTests
{
    [Fact]
    public static void HandlesSource()
    {
        // arrange
        using var source = new Identity<decimal?>
        {
            1, -1, 2, -3, 5
        };

        // act
        using var indicator = Indicator.Abs(source);

        // assert
        Assert.Collection(indicator,
            x => Assert.Equal(1, x),
            x => Assert.Equal(1, x),
            x => Assert.Equal(2, x),
            x => Assert.Equal(3, x),
            x => Assert.Equal(5, x));
    }

    [Fact]
    public static void UpdatesFromSource()
    {
        // arrange
        using var source = new Identity<decimal?>
        {
            1, -1, 2, -3, 5
        };
        using var indicator = Indicator.Abs(source);

        // act
        source.Update(3, -4);

        // assert
        Assert.True(indicator.SequenceEqual(new decimal?[] { 1, 1, 2, 4, 5 }));
    }
}