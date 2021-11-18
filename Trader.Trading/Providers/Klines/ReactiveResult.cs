using Orleans.Concurrency;
using Outcompute.Trader.Models.Collections;

namespace Outcompute.Trader.Trading.Providers.Klines;

[Immutable]
internal readonly record struct ReactiveResult(Guid Version, int Serial, KlineCollection Items);