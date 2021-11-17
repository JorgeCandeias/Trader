using Orleans.Concurrency;
using Outcompute.Trader.Models;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Outcompute.Trader.Trading.Algorithms;

public interface IAutoPositionResolver
{
    Task<AutoPosition> ResolveAsync(Symbol symbol, DateTime startTime, CancellationToken cancellationToken = default);
}

[Immutable]
public record AutoPosition(Symbol Symbol, ImmutableSortedSet<Position> Positions, ImmutableList<ProfitEvent> ProfitEvents, ImmutableList<CommissionEvent> CommissionEvents)
{
    public static AutoPosition Empty { get; } = new AutoPosition(
        Symbol.Empty,
        ImmutableSortedSet<Position>.Empty.WithComparer(PositionKeyComparer.Default),
        ImmutableList<ProfitEvent>.Empty,
        ImmutableList<CommissionEvent>.Empty);
}

[Immutable]
public record Position(string Symbol, long OrderId, decimal Price, decimal Quantity, DateTime Time)
{
    public static Position Empty { get; } = new Position(string.Empty, 0, 0, 0, DateTime.MinValue);
}

public sealed class PositionKeyComparer : IComparer<Position>, IEqualityComparer<Position>
{
    public int Compare(Position? x, Position? y)
    {
        if (x is null) throw new ArgumentNullException(nameof(x));
        if (y is null) throw new ArgumentNullException(nameof(y));

        var bySymbol = string.CompareOrdinal(x.Symbol, y.Symbol);
        if (bySymbol is not 0)
        {
            return bySymbol;
        }

        return x.OrderId.CompareTo(y.OrderId);
    }

    public bool Equals(Position? x, Position? y)
    {
        return x is null ? y is null : y is not null && string.Equals(x.Symbol, y.Symbol, StringComparison.Ordinal) && x.OrderId == y.OrderId;
    }

    public int GetHashCode([DisallowNull] Position obj)
    {
        if (obj is null)
        {
            throw new ArgumentNullException(nameof(obj));
        }

        return HashCode.Combine(obj.Symbol, obj.OrderId);
    }

    public static PositionKeyComparer Default { get; } = new PositionKeyComparer();
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