using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Trader.Data;
using Trader.Models;

namespace Trader.Trading
{
    public interface ITradingService
    {
        Task<ExchangeInfo> GetExchangeInfoAsync(CancellationToken cancellationToken = default);

        Task<SymbolPriceTicker> GetSymbolPriceTickerAsync(string symbol, CancellationToken cancellationToken = default);

        Task<SortedTradeSet> GetAccountTradesAsync(GetAccountTrades model, CancellationToken cancellationToken = default);

        // todo: refactor this to return sorted order set
        Task<ImmutableList<OrderQueryResult>> GetOpenOrdersAsync(GetOpenOrders model, CancellationToken cancellationToken = default);

        // todo: refactor this to return sorted order set
        Task<OrderQueryResult> GetOrderAsync(OrderQuery model, CancellationToken cancellationToken = default);

        Task<SortedOrderSet> GetAllOrdersAsync(GetAllOrders model, CancellationToken cancellationToken = default);

        Task<CancelStandardOrderResult> CancelOrderAsync(CancelStandardOrder model, CancellationToken cancellationToken = default);

        Task<OrderResult> CreateOrderAsync(Order model, CancellationToken cancellationToken = default);

        Task<AccountInfo> GetAccountInfoAsync(GetAccountInfo model, CancellationToken cancellationToken = default);
    }
}