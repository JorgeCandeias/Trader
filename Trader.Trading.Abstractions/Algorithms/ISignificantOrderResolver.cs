using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Trader.Models.Collections;

namespace Trader.Trading.Algorithms
{
    public interface ISignificantOrderResolver
    {
        Task<SignificantResult> ResolveAsync(string symbol, CancellationToken cancellationToken = default);
    }

    public record SignificantResult(ImmutableSortedOrderSet Orders, Profit Profit);

    public record Profit(

        // fixed windows
        decimal Today, decimal Yesterday, decimal ThisWeek, decimal PrevWeek, decimal ThisMonth, decimal ThisYear,

        // rolling windows
        decimal D1, decimal D7, decimal D30)
    {
        public static Profit Zero { get; } = new Profit(0, 0, 0, 0, 0, 0, 0, 0, 0);

        public static Profit Aggregate(IEnumerable<Profit> items)
        {
            var today = 0m;
            var yesterday = 0m;
            var thisWeek = 0m;
            var prevWeek = 0m;
            var thisMonth = 0m;
            var thisYear = 0m;
            var d1 = 0m;
            var d7 = 0m;
            var d30 = 0m;

            foreach (var item in items)
            {
                today += item.Today;
                yesterday += item.Yesterday;
                thisWeek += item.ThisWeek;
                prevWeek += item.PrevWeek;
                thisMonth += item.ThisMonth;
                thisYear += item.ThisYear;
                d1 += item.D1;
                d7 += item.D7;
                d30 += item.D30;
            }

            return new Profit(today, yesterday, thisWeek, prevWeek, thisMonth, thisYear, d1, d7, d30);
        }

        public Profit Add(Profit item)
        {
            return new Profit(
                Today + item.Today,
                Yesterday + item.Yesterday,
                ThisWeek + item.ThisWeek,
                PrevWeek + item.PrevWeek,
                ThisMonth + item.ThisMonth,
                ThisYear + item.ThisYear,
                D1 + item.D1,
                D7 + item.D7,
                D30 + item.D30);
        }
    }

    public record Statistics(decimal AvgPerHourDay1, decimal AvgPerHourDay7, decimal AvgPerHourDay30, decimal AvgPerDay1, decimal AvgPerDay7, decimal AvgPerDay30)
    {
        public static Statistics Zero { get; } = new Statistics(0, 0, 0, 0, 0, 0);

        public static Statistics FromProfit(Profit profit)
        {
            return new Statistics(
                profit.D1 / 24m,
                profit.D7 / (24m * 7m),
                profit.D30 / (24m * 30m),
                profit.D1,
                profit.D7 / 7m,
                profit.D30 / 30m);
        }
    }
}