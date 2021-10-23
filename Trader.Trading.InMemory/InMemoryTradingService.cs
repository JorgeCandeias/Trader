using Orleans;
using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.InMemory
{
    internal class InMemoryTradingService : IInMemoryTradingService
    {
        private readonly IInMemoryTradingServiceGrain _grain;

        public InMemoryTradingService(IGrainFactory factory)
        {
            _grain = factory.GetInMemoryTradingServiceGrain();
        }

        #region Orders

        public Task<CancelStandardOrderResult> CancelOrderAsync(string symbol, long orderId, CancellationToken cancellationToken = default)
        {
            return _grain.CancelOrderAsync(symbol, orderId);
        }

        public Task<OrderResult> CreateOrderAsync(string symbol, OrderSide side, OrderType type, TimeInForce? timeInForce, decimal? quantity, decimal? quoteOrderQuantity, decimal? price, string? newClientOrderId, decimal? stopPrice, decimal? icebergQuantity, CancellationToken cancellationToken = default)
        {
            return _grain.CreateOrderAsync(symbol, side, type, timeInForce, quantity, quoteOrderQuantity, price, newClientOrderId, stopPrice, icebergQuantity);
        }

        public Task<IReadOnlyCollection<OrderQueryResult>> GetAllOrdersAsync(string symbol, long? orderId, int? limit, CancellationToken cancellationToken = default)
        {
            return _grain.GetAllOrdersAsync(symbol, orderId, limit);
        }

        public Task<IReadOnlyCollection<OrderQueryResult>> GetOpenOrdersAsync(string symbol, CancellationToken cancellationToken = default)
        {
            return _grain.GetOpenOrdersAsync(symbol);
        }

        public Task<OrderQueryResult> GetOrderAsync(string symbol, long? orderId, string? originalClientOrderId, CancellationToken cancellationToken = default)
        {
            return _grain.GetOrderAsync(symbol, orderId, originalClientOrderId);
        }

        #endregion Orders

        #region Exchange

        public Task<ExchangeInfo> GetExchangeInfoAsync(CancellationToken cancellationToken = default)
        {
            return _grain.GetExchangeInfoAsync();
        }

        public Task SetExchangeInfoAsync(ExchangeInfo info, CancellationToken cancellationToken = default)
        {
            if (info is null) throw new ArgumentNullException(nameof(info));

            return _grain.SetExchangeInfoAsync(info);
        }

        #endregion Exchange

        public Task CloseUserDataStreamAsync(string listenKey, CancellationToken cancellationToken = default)
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

        public IReadOnlyCollection<SavingsProduct> GetCachedFlexibleProductsByAsset(string asset)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<SavingsProduct>> GetFlexibleProductListAsync(SavingsStatus status, SavingsFeatured featured, long? current, long? size, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<SavingsPosition>> GetFlexibleProductPositionsAsync(string asset, CancellationToken cancellationToken = default)
        {
            return _grain.GetFlexibleProductPositionsAsync(asset);
        }

        public Task<IReadOnlyCollection<Kline>> GetKlinesAsync(string symbol, KlineInterval interval, DateTime startTime, DateTime endTime, int limit, CancellationToken cancellationToken = default)
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

        public Task RedeemFlexibleProductAsync(string productId, decimal amount, SavingsRedemptionType type, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task SetFlexibleProductPositionsAsync(IEnumerable<SavingsPosition> items)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));

            return _grain.SetFlexibleProductPositionsAsync(items);
        }

        public Task SetLeftDailyRedemptionQuotaOnFlexibleProductAsync(string productId, SavingsRedemptionType type, SavingsQuota item)
        {
            if (productId is null) throw new ArgumentNullException(nameof(productId));
            if (item is null) throw new ArgumentNullException(nameof(item));

            return _grain.SetLeftDailyRedemptionQuotaOnFlexibleProductAsync(productId, type, item);
        }

        public Task<SavingsQuota?> TryGetLeftDailyRedemptionQuotaOnFlexibleProductAsync(string productId, SavingsRedemptionType type, CancellationToken cancellationToken = default)
        {
            return _grain.TryGetLeftDailyRedemptionQuotaOnFlexibleProductAsync(productId, type);
        }

        public ITradingService WithBackoff() => this;
    }
}