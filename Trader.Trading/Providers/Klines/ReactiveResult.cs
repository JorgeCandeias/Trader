using Orleans.Concurrency;

namespace Outcompute.Trader.Trading.Providers.Klines;

[Immutable]
internal readonly record struct ReactiveResult(Guid Version, int Serial, ImmutableSortedSet<Kline> Items);