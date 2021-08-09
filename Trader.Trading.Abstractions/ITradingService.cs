﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Trader.Models;
using Trader.Models.Collections;

namespace Trader.Trading
{
    public interface ITradingService
    {
        Task<ExchangeInfo> GetExchangeInfoAsync(CancellationToken cancellationToken = default);

        Task<SymbolPriceTicker> GetSymbolPriceTickerAsync(string symbol, CancellationToken cancellationToken = default);

        Task<ImmutableSortedTradeSet> GetAccountTradesAsync(GetAccountTrades model, CancellationToken cancellationToken = default);

        Task<ImmutableSortedOrderSet> GetOpenOrdersAsync(GetOpenOrders model, CancellationToken cancellationToken = default);

        Task<OrderQueryResult> GetOrderAsync(OrderQuery model, CancellationToken cancellationToken = default);

        Task<ImmutableSortedOrderSet> GetAllOrdersAsync(GetAllOrders model, CancellationToken cancellationToken = default);

        Task<CancelStandardOrderResult> CancelOrderAsync(CancelStandardOrder model, CancellationToken cancellationToken = default);

        Task<OrderResult> CreateOrderAsync(Order model, CancellationToken cancellationToken = default);

        Task<AccountInfo> GetAccountInfoAsync(GetAccountInfo model, CancellationToken cancellationToken = default);

        Task<Ticker> Get24hTickerPriceChangeStatisticsAsync(string symbol, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<Kline>> GetKlinesAsync(GetKlines model, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<FlexibleProductPosition>> GetFlexibleProductPositionAsync(
            string asset,
            CancellationToken cancellationToken = default);

        Task<LeftDailyRedemptionQuotaOnFlexibleProduct?> GetLeftDailyRedemptionQuotaOnFlexibleProductAsync(
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