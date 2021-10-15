using Xunit;

namespace Outcompute.Trader.Trading.Binance.Tests.Fixtures
{
    [CollectionDefinition(nameof(ClusterCollectionFixture))]
    public class ClusterCollectionFixture : ICollectionFixture<ClusterFixture>
    {
    }
}