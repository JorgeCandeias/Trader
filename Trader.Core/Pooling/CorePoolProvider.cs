using Microsoft.Extensions.ObjectPool;

namespace Outcompute.Trader.Core.Pooling;

internal static class CorePoolProvider
{
    public static ObjectPoolProvider Default { get; } = new DefaultObjectPoolProvider();
}