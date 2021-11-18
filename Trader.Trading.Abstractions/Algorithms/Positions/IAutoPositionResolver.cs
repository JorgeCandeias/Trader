using Orleans.Concurrency;
using Outcompute.Trader.Models;
using System.Collections.Immutable;

namespace Outcompute.Trader.Trading.Algorithms.Positions;

public interface IAutoPositionResolver
{
    Task<AutoPosition> ResolveAsync(Symbol symbol, DateTime startTime, CancellationToken cancellationToken = default);
}

[Immutable]
public record AutoPosition(Symbol Symbol, PositionCollection Positions, ImmutableList<ProfitEvent> ProfitEvents, ImmutableList<CommissionEvent> CommissionEvents)
{
    public static AutoPosition Empty { get; } = new AutoPosition(
        Symbol.Empty,
        PositionCollection.Empty,
        ImmutableList<ProfitEvent>.Empty,
        ImmutableList<CommissionEvent>.Empty);
}

public record Stats(decimal AvgPerHourDay1, decimal AvgPerHourDay7, decimal AvgPerHourDay30, decimal AvgPerDay1, decimal AvgPerDay7, decimal AvgPerDay30)
{
    public static Stats Zero { get; } = new Stats(0, 0, 0, 0, 0, 0);

    public static Stats FromProfit(Profit profit)
    {
        if (profit is null) throw new ArgumentNullException(nameof(profit));

        return new Stats(
            profit.D1 / 24m,
            profit.D7 / (24m * 7m),
            profit.D30 / (24m * 30m),
            profit.D1,
            profit.D7 / 7m,
            profit.D30 / 30m);
    }
}