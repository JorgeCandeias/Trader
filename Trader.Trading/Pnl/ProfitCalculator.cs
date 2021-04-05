using System;
using System.Linq;
using Trader.Core.Time;
using Trader.Data;
using Trader.Trading.Algorithms;

namespace Trader.Trading.Pnl
{
    internal class ProfitCalculator : IProfitCalculator
    {
        private readonly ISystemClock _clock;
        private readonly IOrderCodeGenerator _orderCodeGenerator;

        public ProfitCalculator(ISystemClock clock, IOrderCodeGenerator orderCodeGenerator)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _orderCodeGenerator = orderCodeGenerator ?? throw new ArgumentNullException(nameof(orderCodeGenerator));
        }

        public Profit Calculate(SortedTradeSet trades)
        {
            _ = trades ?? throw new ArgumentNullException(nameof(trades));

            // hold the current time so profit assignments are consistent
            var today = _clock.UtcNow.Date;

            // prepare for indexing
            var ordered = trades.ToList();

            // keep track of fullfilment
            var quantities = ordered.ToDictionary(x => x.Id, x => x.Quantity);

            // enumerate the sales in ascending order
            var todayProfit = 0m;
            var yesterdayProfit = 0m;
            var thisWeekProfit = 0m;
            var thisMonthProfit = 0m;
            var thisYearProfit = 0m;

            for (var i = 0; i < ordered.Count; ++i)
            {
                var sell = ordered[i];
                if (!sell.IsBuyer)
                {
                    // attempt to find the paired buys
                    // todo: this requires the client order ids to be stored on the database

                    // enumerate the buys in descending order from the sale
                    for (var j = i - 1; j >= 0; --j)
                    {
                        var buy = ordered[j];
                        if (buy.IsBuyer)
                        {
                            // take as much as possible of the sale from the buy
                            var take = Math.Min(quantities[buy.Id], quantities[sell.Id]);
                            quantities[buy.Id] -= take;
                            quantities[sell.Id] -= take;

                            // calculate profit for this
                            var profit = take * (sell.Price - buy.Price);

                            // assign to the appropriate counters
                            if (sell.Time.Date == today) todayProfit += profit;
                            if (sell.Time.Date == today.AddDays(-1)) yesterdayProfit += profit;
                            if (sell.Time.Date >= today.Previous(DayOfWeek.Sunday)) thisWeekProfit += profit;
                            if (sell.Time.Date >= today.AddDays(-today.Day + 1)) thisMonthProfit += profit;
                            if (sell.Time.Date >= new DateTime(today.Year, 1, 1)) thisYearProfit += profit;
                        }
                    }
                }
            }

            return new Profit(todayProfit, yesterdayProfit, thisWeekProfit, thisMonthProfit, thisYearProfit);
        }
    }
}