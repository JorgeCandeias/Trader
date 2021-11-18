using Orleans.Concurrency;
using Outcompute.Trader.Models;
using System.Collections.Immutable;

namespace Outcompute.Trader.Trading.Algorithms.Positions
{
    [Immutable]
    public record AutoPosition(Symbol Symbol, PositionCollection Positions, ImmutableList<ProfitEvent> ProfitEvents, ImmutableList<CommissionEvent> CommissionEvents)
    {
        public static AutoPosition Empty { get; } = new AutoPosition(
            Symbol.Empty,
            PositionCollection.Empty,
            ImmutableList<ProfitEvent>.Empty,
            ImmutableList<CommissionEvent>.Empty);
    }
}