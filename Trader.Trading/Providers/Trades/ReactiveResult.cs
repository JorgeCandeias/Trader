namespace Outcompute.Trader.Trading.Providers.Trades;

internal readonly record struct ReactiveResult(
    Guid Version,
    int Serial,
    ImmutableSortedSet<AccountTrade> Trades);