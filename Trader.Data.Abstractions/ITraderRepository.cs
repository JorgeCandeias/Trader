﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Trader.Models;
using Trader.Models.Collections;

namespace Trader.Data
{
    public interface ITraderRepository
    {
        Task<long> GetLastPagedOrderIdAsync(string symbol, CancellationToken cancellationToken = default);

        Task SetLastPagedOrderIdAsync(string symbol, long orderId, CancellationToken cancellationToken = default);

        Task<ImmutableSortedOrderSet> GetOrdersAsync(string symbol, CancellationToken cancellationToken = default);

        Task SetOrdersAsync(IEnumerable<OrderQueryResult> orders, CancellationToken cancellationToken = default);

        Task SetOrderAsync(OrderQueryResult order, CancellationToken cancellationToken = default);

        Task SetOrderAsync(CancelStandardOrderResult result, CancellationToken cancellationToken = default);

        Task SetOrderAsync(OrderResult result, decimal stopPrice = 0m, decimal icebergQuantity = 0m, decimal originalQuoteOrderQuantity = 0m, CancellationToken cancellationToken = default);

        Task<long> GetMinTransientOrderIdAsync(string symbol, CancellationToken cancellationToken = default);

        Task<ImmutableSortedOrderSet> GetTransientOrdersBySideAsync(string symbol, OrderSide orderSide, CancellationToken cancellationToken = default);

        Task<ImmutableSortedOrderSet> GetNonSignificantTransientOrdersBySideAsync(string symbol, OrderSide orderSide, CancellationToken cancellationToken = default);

        Task<long> GetMaxTradeIdAsync(string symbol, CancellationToken cancellationToken = default);

        Task SetTradesAsync(IEnumerable<AccountTrade> trades, CancellationToken cancellationToken = default);

        Task<ImmutableSortedTradeSet> GetTradesAsync(string symbol, CancellationToken cancellationToken = default);
    }
}