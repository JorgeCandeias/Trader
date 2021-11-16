using Orleans.Concurrency;

namespace Outcompute.Trader.Trading.Providers;

[Immutable]
public record RedeemSavingsEvent(bool Success, decimal Redeemed);