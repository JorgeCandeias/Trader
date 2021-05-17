using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Trader.Models;
using Trader.Models.Collections;

namespace Trader.Data
{
    public interface ITradingRepository
    {
        Task<long> GetLastPagedOrderIdAsync(string symbol, CancellationToken cancellationToken = default);

        Task SetLastPagedOrderIdAsync(string symbol, long orderId, CancellationToken cancellationToken = default);

        Task<long> GetLastPagedTradeIdAsync(string symbol, CancellationToken cancellationToken = default);

        Task SetLastPagedTradeIdAsync(string symbol, long tradeId, CancellationToken cancellationToken = default);

        Task<OrderQueryResult> GetOrderAsync(string symbol, long orderId, CancellationToken cancellationToken = default);

        Task<OrderQueryResult?> GetLatestOrderBySideAsync(string symbol, OrderSide side, CancellationToken cancellationToken = default);

        Task<ImmutableSortedOrderSet> GetOrdersAsync(string symbol, CancellationToken cancellationToken = default);

        Task<ImmutableSortedOrderSet> GetSignificantCompletedOrdersAsync(string symbol, CancellationToken cancellationToken = default);

        Task SetOrdersAsync(IEnumerable<OrderQueryResult> orders, CancellationToken cancellationToken = default);

        Task SetOrderAsync(OrderQueryResult order, CancellationToken cancellationToken = default);

        Task SetOrderAsync(CancelStandardOrderResult result, CancellationToken cancellationToken = default);

        Task SetOrderAsync(OrderResult result, decimal stopPrice = 0m, decimal icebergQuantity = 0m, decimal originalQuoteOrderQuantity = 0m, CancellationToken cancellationToken = default);

        Task<long> GetMinTransientOrderIdAsync(string symbol, CancellationToken cancellationToken = default);

        Task<ImmutableSortedOrderSet> GetTransientOrdersBySideAsync(string symbol, OrderSide orderSide, CancellationToken cancellationToken = default);

        Task<ImmutableSortedOrderSet> GetNonSignificantTransientOrdersBySideAsync(string symbol, OrderSide orderSide, CancellationToken cancellationToken = default);

        Task<long> GetMaxTradeIdAsync(string symbol, CancellationToken cancellationToken = default);

        Task SetTradeAsync(AccountTrade trade, CancellationToken cancellationToken = default);

        Task SetTradesAsync(IEnumerable<AccountTrade> trades, CancellationToken cancellationToken = default);

        Task<ImmutableSortedTradeSet> GetTradesAsync(string symbol, CancellationToken cancellationToken = default);

        Task SetBalancesAsync(AccountInfo accountInfo, CancellationToken cancellationToken = default);

        Task SetBalancesAsync(IEnumerable<Balance> balances, CancellationToken cancellationToken = default);

        Task<Balance> GetBalanceAsync(string asset, CancellationToken cancellationToken = default);

        Task SetTickersAsync(IEnumerable<MiniTicker> tickers, CancellationToken cancellationToken = default);

        Task<MiniTicker> GetTickerAsync(string symbol, CancellationToken cancellationToken = default);

        Task SetCandlesticksAsync(IEnumerable<Candlestick> candlesticks, CancellationToken cancellationToken = default);
    }
}