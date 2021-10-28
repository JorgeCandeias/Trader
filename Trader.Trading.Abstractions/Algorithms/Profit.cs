using Orleans.Concurrency;
using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Outcompute.Trader.Trading.Algorithms
{
    [Immutable]
    public record Profit(

        // identifiers
        string Symbol,
        string Asset,
        string Quote,

        // fixed windows
        decimal Today, decimal Yesterday, decimal ThisWeek, decimal PrevWeek, decimal ThisMonth, decimal ThisYear, decimal All,

        // rolling windows
        decimal D1, decimal D7, decimal D30)
    {
        public static Profit Zero(string symbol, string asset, string quote)
        {
            return new Profit(symbol, asset, quote, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        }

        public static Profit Aggregate(IEnumerable<Profit> items)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));

            var today = 0m;
            var yesterday = 0m;
            var thisWeek = 0m;
            var prevWeek = 0m;
            var thisMonth = 0m;
            var thisYear = 0m;
            var all = 0m;
            var d1 = 0m;
            var d7 = 0m;
            var d30 = 0m;

            string? quote = null;
            string? asset = null;
            string? symbol = null;
            var first = true;

            foreach (var item in items)
            {
                if (first)
                {
                    quote = item.Quote;
                    asset = item.Asset;
                    symbol = item.Symbol;

                    first = false;
                }
                else
                {
                    if (item.Quote != quote) throw new InvalidOperationException($"Cannot aggregate profit from different quotes '{quote}' and '{item.Quote}'");

                    if (item.Asset != asset) asset = null;
                    if (item.Symbol != symbol) symbol = null;
                }

                today += item.Today;
                yesterday += item.Yesterday;
                thisWeek += item.ThisWeek;
                prevWeek += item.PrevWeek;
                thisMonth += item.ThisMonth;
                thisYear += item.ThisYear;
                all += item.All;
                d1 += item.D1;
                d7 += item.D7;
                d30 += item.D30;
            }

            return new Profit(symbol ?? string.Empty, asset ?? string.Empty, quote ?? string.Empty, today, yesterday, thisWeek, prevWeek, thisMonth, thisYear, all, d1, d7, d30);
        }

        public Profit Add(Profit item)
        {
            if (item is null) throw new ArgumentNullException(nameof(item));
            if (item.Quote != Quote) throw new InvalidOperationException($"Cannot aggregate profit from different quotes '{Quote}' and '{item.Quote}'");

            return new Profit(
                Symbol == item.Symbol ? Symbol : string.Empty,
                Asset == item.Asset ? Asset : string.Empty,
                Quote,
                Today + item.Today,
                Yesterday + item.Yesterday,
                ThisWeek + item.ThisWeek,
                PrevWeek + item.PrevWeek,
                ThisMonth + item.ThisMonth,
                ThisYear + item.ThisYear,
                All + item.All,
                D1 + item.D1,
                D7 + item.D7,
                D30 + item.D30);
        }

        public static Profit FromEvents(Symbol symbol, IEnumerable<ProfitEvent> profits, IEnumerable<CommissionEvent> commissions, DateTime now)
        {
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));
            if (profits is null) throw new ArgumentNullException(nameof(profits));
            if (commissions is null) throw new ArgumentNullException(nameof(commissions));

            var lookup = commissions.ToLookup(x => (x.Asset, x.OrderId, x.TradeId));

            var todayProfit = 0m;
            var yesterdayProfit = 0m;
            var thisWeekProfit = 0m;
            var prevWeekProfit = 0m;
            var thisMonthProfit = 0m;
            var thisYearProfit = 0m;
            var all = 0m;
            var d1 = 0m;
            var d7 = 0m;
            var d30 = 0m;

            // hold the current time so profit assignments are consistent
            var window1d = now.AddDays(-1);
            var window7d = now.AddDays(-7);
            var window30d = now.AddDays(-30);
            var today = now.Date;

            foreach (var profit in profits)
            {
                // apply this profit event
                Apply(profit, profit.Profit);

                // if the commission was taken from the sell asset then remove it from profit
                foreach (var loss in lookup[(profit.Symbol.QuoteAsset, profit.SellOrderId, profit.SellTradeId)].Select(x => x.Commission))
                {
                    Apply(profit, -loss);
                }
            }

            return new Profit(
                symbol.Name,
                symbol.BaseAsset,
                symbol.QuoteAsset,
                todayProfit,
                yesterdayProfit,
                thisWeekProfit,
                prevWeekProfit,
                thisMonthProfit,
                thisYearProfit,
                all,
                d1,
                d7,
                d30);

            void Apply(ProfitEvent profit, decimal value)
            {
                // assign to the appropriate counters
                if (profit.EventTime.Date == today) todayProfit += value;
                if (profit.EventTime.Date == today.AddDays(-1)) yesterdayProfit += value;
                if (profit.EventTime.Date >= today.Previous(DayOfWeek.Sunday)) thisWeekProfit += value;
                if (profit.EventTime.Date >= today.Previous(DayOfWeek.Sunday, 2) && profit.EventTime.Date < today.Previous(DayOfWeek.Sunday)) prevWeekProfit += value;
                if (profit.EventTime.Date >= today.AddDays(-today.Day + 1)) thisMonthProfit += value;
                if (profit.EventTime.Date >= new DateTime(today.Year, 1, 1)) thisYearProfit += value;
                all += value;

                // assign to the window counters
                if (profit.EventTime >= window1d) d1 += value;
                if (profit.EventTime >= window7d) d7 += value;
                if (profit.EventTime >= window30d) d30 += value;
            }
        }
    }
}