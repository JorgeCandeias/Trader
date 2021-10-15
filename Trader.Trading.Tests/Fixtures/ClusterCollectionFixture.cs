using Xunit;

namespace Outcompute.Trader.Trading.Tests.Fixtures
{
    [CollectionDefinition(nameof(ClusterCollectionFixture))]
    public class ClusterCollectionFixture : ICollectionFixture<ClusterFixture>
    {
    }
}