using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Providers.Exchange;

internal readonly record struct ExchangeInfoResult(
    ExchangeInfo Value,
    Guid Version);