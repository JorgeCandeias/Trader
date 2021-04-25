using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Trader.Data
{
    public interface ITraderRepository
    {
        Task<long> GetLastPagedOrderIdAsync(string symbol, CancellationToken cancellationToken = default);

        Task SetLastPagedOrderIdAsync(string symbol, long orderId, CancellationToken cancellationToken = default);

        Task<OrderQueryResult?> GetOrderAsync(string symbol, long orderId, CancellationToken cancellationToken = default);

        Task<SortedOrderSet> GetOrdersAsync(string symbol, CancellationToken cancellationToken = default);

        Task SetOrderAsync(OrderQueryResult order, CancellationToken cancellationToken = default);

        Task SetOrdersAsync(IEnumerable<OrderQueryResult> orders, CancellationToken cancellationToken = default);

        Task SetOrderAsync(CancelStandardOrderResult result, CancellationToken cancellationToken = default);

        Task SetOrderAsync(OrderResult result, CancellationToken cancellationToken = default);

        Task<long> GetMinTransientOrderIdAsync(string symbol, CancellationToken cancellationToken = default);

        Task<SortedOrderSet> GetTransientOrdersAsync(string symbol, OrderSide? orderSide = default, bool? significant = default, CancellationToken cancellationToken = default);

        Task<long> GetMaxTradeIdAsync(string symbol, CancellationToken cancellationToken = default);

        Task SetTradeAsync(AccountTrade trade, CancellationToken cancellationToken = default);

        Task SetTradesAsync(IEnumerable<AccountTrade> trades, CancellationToken cancellationToken = default);

        Task<SortedTradeSet> GetTradesAsync(string symbol, CancellationToken cancellationToken = default);
    }
}