namespace Outcompute.Trader.Models;

public record SymbolOrderBookTicker(
    string Symbol,
    decimal BidPrice,
    decimal BidQuantity,
    decimal AskPrice,
    decimal AskQuantity);