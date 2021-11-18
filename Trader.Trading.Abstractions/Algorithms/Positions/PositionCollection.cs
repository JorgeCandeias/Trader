using Orleans.Concurrency;
using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace Outcompute.Trader.Trading.Algorithms.Positions;

[Immutable]
public class PositionCollection : ReadOnlyCollection<Position>
{
    public PositionCollection(IList<Position> list) : base(list)
    {
    }

    public Position First => Items[0];

    public Position Last => Items[^1];

    public static PositionCollection Empty { get; } = new PositionCollection(ImmutableList<Position>.Empty);
}