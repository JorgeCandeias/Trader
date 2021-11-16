using Orleans.Concurrency;
using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Algorithms;

[Immutable]
public record CommissionEvent(
    Symbol Symbol,
    DateTime EventTime,
    long OrderId,
    long TradeId,
    string Asset,
    decimal Commission);