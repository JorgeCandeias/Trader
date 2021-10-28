using Orleans.Concurrency;
using Outcompute.Trader.Models;
using System;

namespace Outcompute.Trader.Trading.Algorithms
{
    [Immutable]
    public record ProfitEvent(
        Symbol Symbol,
        DateTime EventTime,
        long BuyOrderId,
        long BuyTradeId,
        long SellOrderId,
        long SellTradeId,
        decimal Quantity,
        decimal BuyPrice,
        decimal SellPrice)
    {
        public decimal BuyValue => Quantity * BuyPrice;
        public decimal SellValue => Quantity * SellPrice;
        public decimal Profit => (SellPrice - BuyPrice) * Quantity;
    }
}