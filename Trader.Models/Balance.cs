using Orleans.Concurrency;

namespace Outcompute.Trader.Models;

[Immutable]
public record Balance(
    string Asset,
    decimal Free,
    decimal Locked,
    DateTime UpdatedTime)
{
    public decimal Total => Free + Locked;

    public static Balance Empty { get; } = new Balance(string.Empty, 0m, 0m, DateTime.MinValue);

    public static Balance Zero(string asset) => new(asset, 0m, 0m, DateTime.MinValue);
}