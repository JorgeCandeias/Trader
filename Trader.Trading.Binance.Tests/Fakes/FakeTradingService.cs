using Orleans;
using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Binance.Tests.Fakes
{
    internal class FakeTradingService : ITradingService
    {
        private readonly IFakeTradingServiceGrain _grain;

        public FakeTradingService(IGrainFactory factory)
        {
            _grain = factory.GetFakeTradingServiceGrain();
        }

        public Task<CancelStandardOrderResult> CancelOrderAsync(string symbol, long orderId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task CloseUserDataStreamAsync(string listenKey, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<OrderResult> CreateOrderAsync(string symbol, OrderSide side, OrderType type, TimeInForce? timeInForce, decimal? quantity, decimal? quoteOrderQuantity, decimal? price, string? newClientOrderId, decimal? stopPrice, decimal? icebergQuantity, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<string> CreateUserDataStreamAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Ticker> Get24hTickerPriceChangeStatisticsAsync(string symbol, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<AccountInfo> GetAccountInfoAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ImmutableSortedTradeSet> GetAccountTradesAsync(string symbol, long? fromId, int? limit, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ImmutableSortedOrderSet> GetAllOrdersAsync(string symbol, long? orderId, int? limit, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyCollection<FlexibleProduct> GetCachedFlexibleProductsByAsset(string asset)
        {
            throw new NotImplementedException();
        }

        public Task<ExchangeInfo> GetExchangeInfoAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<FlexibleProduct>> GetFlexibleProductListAsync(FlexibleProductStatus status, FlexibleProductFeatured featured, long? current, long? size, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<FlexibleProductPosition>> GetFlexibleProductPositionsAsync(string asset, CancellationToken cancellationToken = default)
        {
            return _grain.GetFlexibleProductPositionsAsync(asset);
        }

        public Task<IReadOnlyCollection<Kline>> GetKlinesAsync(string symbol, KlineInterval interval, DateTime startTime, DateTime endTime, int limit, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ImmutableSortedOrderSet> GetOpenOrdersAsync(string symbol, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<OrderQueryResult> GetOrderAsync(string symbol, long? orderId, string? originalClientOrderId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<SymbolPriceTicker> GetSymbolPriceTickerAsync(string symbol, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task PingUserDataStreamAsync(string listenKey, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task RedeemFlexibleProductAsync(string productId, decimal amount, FlexibleProductRedemptionType type, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<LeftDailyRedemptionQuotaOnFlexibleProduct?> TryGetLeftDailyRedemptionQuotaOnFlexibleProductAsync(string productId, FlexibleProductRedemptionType type, CancellationToken cancellationToken = default)
        {
            return _grain.TryGetLeftDailyRedemptionQuotaOnFlexibleProductAsync(productId, type);
        }

        public ITradingService WithBackoff() => this;
    }
}