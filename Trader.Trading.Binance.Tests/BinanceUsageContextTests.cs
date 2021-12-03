using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Binance.Tests;

public class BinanceUsageContextTests
{
    [Fact]
    public void SetsAndGets()
    {
        // arrange
        var context = new BinanceUsageContext();
        var type = RateLimitType.RequestWeight;
        var window = TimeSpan.FromMinutes(1);
        var limit = 100;
        var used = 10;
        var updated = DateTime.UtcNow;

        // act
        context.SetLimit(type, window, limit);
        context.SetUsed(type, window, used, updated);
        var result = context.EnumerateAll();

        // assert
        Assert.Collection(result,
            x =>
            {
                Assert.Equal(type, x.Type);
                Assert.Equal(window, x.Window);
                Assert.Equal(used, x.Used);
                Assert.Equal(updated, x.Updated);
                Assert.Equal(limit, x.Limit);
            });
    }
}