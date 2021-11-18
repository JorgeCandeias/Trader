using System.Collections.Immutable;

namespace Outcompute.Trader.Trading.Algorithms.Positions;

public static class PositionCollectionEnumerableExtensions
{
    public static PositionCollection ToPositionCollection(this IEnumerable<Position> positions)
    {
        var list = positions.ToImmutableList();

        return new PositionCollection(list);
    }
}