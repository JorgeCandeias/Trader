using Orleans.Concurrency;

namespace Outcompute.Trader.Models;

/// <summary>
/// Spot balances for an asset.
/// </summary>
/// <param name="Asset">The asset the balance refers to.</param>
/// <param name="Free">The free quantity part of the balance.</param>
/// <param name="Locked">The locked quantity part of the balance.</param>
/// <param name="UpdatedTime">The last time the balance was updated.</param>
[Immutable]
public record Balance(
    string Asset,
    decimal Free,
    decimal Locked,
    DateTime UpdatedTime)
{
    /// <summary>
    /// The total balance including free and locked quantities.
    /// </summary>
    public decimal Total => Free + Locked;

    /// <summary>
    /// Returns a zero balance instance without an asset defined.
    /// </summary>
    public static Balance Empty { get; } = new Balance(string.Empty, 0m, 0m, DateTime.MinValue);

    /// <summary>
    /// Returns a zero balance instance for the specifed asset.
    /// </summary>
    public static Balance Zero(string asset) => new(asset, 0m, 0m, DateTime.MinValue);
}