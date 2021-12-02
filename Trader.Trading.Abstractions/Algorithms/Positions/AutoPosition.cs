using Orleans.Concurrency;
using Outcompute.Trader.Models;
using System.Collections.Immutable;

namespace Outcompute.Trader.Trading.Algorithms.Positions
{
    [Immutable]
    public record AutoPosition()
    {
        public Symbol Symbol { get; init; } = Symbol.Empty;
        public PositionCollection Positions { get; init; } = PositionCollection.Empty;
        public ImmutableList<ProfitEvent> ProfitEvents { get; init; } = ImmutableList<ProfitEvent>.Empty;
        public ImmutableList<CommissionEvent> CommissionEvents { get; init; } = ImmutableList<CommissionEvent>.Empty;

        public static AutoPosition Empty { get; } = new AutoPosition();
    }
}