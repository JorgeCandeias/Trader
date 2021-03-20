using System;
using System.Collections.Generic;
using System.Linq;
using Trader.Data;

namespace Trader.Trading.Algorithms
{
    /// <summary>
    /// Maps an order to any resulting trades.
    /// </summary>
    internal class OrderTradeMap
    {
        public OrderTradeMap(OrderQueryResult order, IEnumerable<AccountTrade> trades)
        {
            Order = order ?? throw new ArgumentNullException(nameof(order));
            Trades = trades ?? throw new ArgumentNullException(nameof(trades));

            MaxTradeTime = Trades.Any() ? Trades.Max(x => x.Time) : null;
            MaxEventTime = MaxTradeTime ?? Order.Time;

            RemainingExecutedQuantity = Order.ExecutedQuantity;
        }

        public OrderQueryResult Order { get; }
        public IEnumerable<AccountTrade> Trades { get; }

        public DateTime? MaxTradeTime { get; }
        public DateTime MaxEventTime { get; }

        public decimal RemainingExecutedQuantity { get; set; }
    }
}