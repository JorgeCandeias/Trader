using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading
{
    public interface ITradingService
    {
        /// <summary>
        /// Enables automatic backoff upon "too many requests" exceptions for the next request as implemented by the provider.
        /// </summary>
        ITradingService WithBackoff();

        Task<ExchangeInfo> GetExchangeInfoAsync(CancellationToken cancellationToken = default);

        Task<SymbolPriceTicker> GetSymbolPriceTickerAsync(string symbol, CancellationToken cancellationToken = default);

        Task<ImmutableSortedTradeSet> GetAccountTradesAsync(string symbol, long? fromId, int? limit, CancellationToken cancellationToken = default);

        Task<ImmutableSortedOrderSet> GetOpenOrdersAsync(string symbol, CancellationToken cancellationToken = default);

        Task<OrderQueryResult> GetOrderAsync(string symbol, long? orderId, string? originalClientOrderId, CancellationToken cancellationToken = default);

        Task<ImmutableSortedOrderSet> GetAllOrdersAsync(string symbol, long? orderId, int? limit, CancellationToken cancellationToken = default);

        Task<CancelStandardOrderResult> CancelOrderAsync(string symbol, long orderId, CancellationToken cancellationToken = default);

        Task<OrderResult> CreateOrderAsync(string symbol, OrderSide side, OrderType type, TimeInForce? timeInForce, decimal? quantity, decimal? quoteOrderQuantity, decimal? price, string? newClientOrderId, decimal? stopPrice, decimal? icebergQuantity, CancellationToken cancellationToken = default);

        Task<AccountInfo> GetAccountInfoAsync(CancellationToken cancellationToken = default);

        Task<Ticker> Get24hTickerPriceChangeStatisticsAsync(string symbol, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<Kline>> GetKlinesAsync(string symbol, KlineInterval interval, DateTime startTime, DateTime endTime, int limit, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<FlexibleProductPosition>> GetFlexibleProductPositionsAsync(
            string asset,
            CancellationToken cancellationToken = default);

        Task<LeftDailyRedemptionQuotaOnFlexibleProduct?> TryGetLeftDailyRedemptionQuotaOnFlexibleProductAsync(
            string productId,
            FlexibleProductRedemptionType type,
            CancellationToken cancellationToken = default);

        Task RedeemFlexibleProductAsync(
            string productId,
            decimal amount,
            FlexibleProductRedemptionType type,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<FlexibleProduct>> GetFlexibleProductListAsync(
           FlexibleProductStatus status,
           FlexibleProductFeatured featured,
           long? current,
           long? size,
           CancellationToken cancellationToken = default);

        IReadOnlyCollection<FlexibleProduct> GetCachedFlexibleProductsByAsset(string asset);

        Task<string> CreateUserDataStreamAsync(CancellationToken cancellationToken = default);

        Task PingUserDataStreamAsync(string listenKey, CancellationToken cancellationToken = default);

        Task CloseUserDataStreamAsync(string listenKey, CancellationToken cancellationToken = default);
    }
}