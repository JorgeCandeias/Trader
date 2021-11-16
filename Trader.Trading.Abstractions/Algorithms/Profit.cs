using Orleans.Concurrency;
using Outcompute.Trader.Models;
using System.Diagnostics.CodeAnalysis;

namespace Outcompute.Trader.Trading.Algorithms;

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

        var builder = CreateBuilder();

        var first = true;

        foreach (var item in items)
        {
            if (first)
            {
                builder.Symbol = item.Symbol;
                builder.Asset = item.Asset;
                builder.Quote = item.Quote;

                first = false;
            }
            else
            {
                if (item.Quote != builder.Quote) throw new InvalidOperationException($"Cannot aggregate profit from different quotes '{builder.Quote}' and '{item.Quote}'");

                if (item.Asset != builder.Asset) builder.Asset = string.Empty;
                if (item.Symbol != builder.Symbol) builder.Symbol = string.Empty;
            }

            builder.Today += item.Today;
            builder.Yesterday += item.Yesterday;
            builder.ThisWeek += item.ThisWeek;
            builder.PrevWeek += item.PrevWeek;
            builder.ThisMonth += item.ThisMonth;
            builder.ThisYear += item.ThisYear;
            builder.All += item.All;
            builder.D1 += item.D1;
            builder.D7 += item.D7;
            builder.D30 += item.D30;
        }

        return builder.ToProfit();
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

        var builder = CreateBuilder();
        builder.Symbol = symbol.Name;
        builder.Asset = symbol.BaseAsset;
        builder.Quote = symbol.QuoteAsset;

        foreach (var profit in profits)
        {
            builder.Add(profit, now);

            // if the commission was taken from the sell asset then remove it from profit
            foreach (var loss in lookup[(profit.Symbol.QuoteAsset, profit.SellOrderId, profit.SellTradeId)].Select(x => x.Commission))
            {
                builder.Add(profit.EventTime, now, -loss);
            }
        }

        return builder.ToProfit();
    }

    [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Builder")]
    public class Builder
    {
        internal Builder()
        {
        }

        // identifiers
        public string Symbol { get; set; } = string.Empty;
        public string Asset { get; set; } = string.Empty;
        public string Quote { get; set; } = string.Empty;

        // fixed windows
        public decimal Today { get; set; }
        public decimal Yesterday { get; set; }
        public decimal ThisWeek { get; set; }
        public decimal PrevWeek { get; set; }
        public decimal ThisMonth { get; set; }
        public decimal ThisYear { get; set; }
        public decimal All { get; set; }

        // rolling windows
        public decimal D1 { get; set; }
        public decimal D7 { get; set; }
        public decimal D30 { get; set; }

        public Profit ToProfit() => new(Symbol, Asset, Quote, Today, Yesterday, ThisWeek, PrevWeek, ThisMonth, ThisYear, All, D1, D7, D30);

        public void Add(ProfitEvent profit, DateTime now)
        {
            if (profit is null)
            {
                throw new ArgumentNullException(nameof(profit));
            }

            // assign to the appropriate counters
            if (profit.EventTime.Date == now.Date) Today += profit.Profit;
            if (profit.EventTime.Date == now.Date.AddDays(-1)) Yesterday += profit.Profit;
            if (profit.EventTime.Date >= now.Date.Previous(DayOfWeek.Sunday)) ThisWeek += profit.Profit;
            if (profit.EventTime.Date >= now.Date.Previous(DayOfWeek.Sunday, 2) && profit.EventTime.Date < now.Date.Previous(DayOfWeek.Sunday)) PrevWeek += profit.Profit;
            if (profit.EventTime.Date >= now.Date.AddDays(-now.Day + 1)) ThisMonth += profit.Profit;
            if (profit.EventTime.Date >= new DateTime(now.Year, 1, 1)) ThisYear += profit.Profit;
            All += profit.Profit;

            // assign to the window counters
            if (profit.EventTime >= now.AddDays(-1)) D1 += profit.Profit;
            if (profit.EventTime >= now.AddDays(-7)) D7 += profit.Profit;
            if (profit.EventTime >= now.AddDays(-30)) D30 += profit.Profit;
        }

        public void Add(DateTime time, DateTime now, decimal value)
        {
            // assign to the appropriate counters
            if (time.Date == now.Date) Today += value;
            if (time.Date == now.Date.AddDays(-1)) Yesterday += value;
            if (time.Date >= now.Date.Previous(DayOfWeek.Sunday)) ThisWeek += value;
            if (time.Date >= now.Date.Previous(DayOfWeek.Sunday, 2) && time.Date < now.Date.Previous(DayOfWeek.Sunday)) PrevWeek += value;
            if (time.Date >= now.Date.AddDays(-now.Day + 1)) ThisMonth += value;
            if (time.Date >= new DateTime(now.Year, 1, 1)) ThisYear += value;
            All += value;

            // assign to the window counters
            if (time >= now.AddDays(-1)) D1 += value;
            if (time >= now.AddDays(-7)) D7 += value;
            if (time >= now.AddDays(-30)) D30 += value;
        }
    }

    public static Builder CreateBuilder() => new();
}