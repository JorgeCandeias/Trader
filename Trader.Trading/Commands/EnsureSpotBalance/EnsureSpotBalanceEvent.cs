namespace Outcompute.Trader.Trading.Commands.EnsureSpotBalance;

internal record EnsureSpotBalanceEvent(bool Success, decimal Redeemed);