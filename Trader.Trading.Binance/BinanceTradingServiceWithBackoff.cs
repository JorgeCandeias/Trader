using Microsoft.Extensions.Logging;
using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;
using Polly;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Binance
{
    internal class BinanceTradingServiceWithBackoff : ITradingService
    {
        private readonly ILogger _logger;
        private readonly ITradingService _trader;

        public BinanceTradingServiceWithBackoff(ILogger<BinanceTradingServiceWithBackoff> logger, ITradingService trader)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _trader = trader ?? throw new ArgumentNullException(nameof(trader));
        }

        private IAsyncPolicy CreatePolicy()
        {
            return Policy
                .Handle<BinanceTooManyRequestsException>()
                .WaitAndRetryForeverAsync(
                    (n, ex, ctx) => ((BinanceTooManyRequestsException)ex).RetryAfter,
                    (ex, ts, ctx) => { _logger.LogWarning(ex, "Backing off for {TimeSpan}...", ts); return Task.CompletedTask; });
        }

        private Task WaitAndRetryForeverAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
        {
            return CreatePolicy().ExecuteAsync(ct => action(ct), cancellationToken, false);
        }

        private Task<TResult> WaitAndRetryForeverAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken cancellationToken = default)
        {
            return CreatePolicy().ExecuteAsync(ct => action(ct), cancellationToken, false);
        }

        public ITradingService WithBackoff() => this;

        public Task<CancelStandardOrderResult> CancelOrderAsync(CancelStandardOrder model, CancellationToken cancellationToken = default)
        {
            return WaitAndRetryForeverAsync(ct => _trader.CancelOrderAsync(model, ct), cancellationToken);
        }

        public Task CloseUserDataStreamAsync(string listenKey, CancellationToken cancellationToken = default)
        {
            return WaitAndRetryForeverAsync(ct => _trader.CloseUserDataStreamAsync(listenKey, ct), cancellationToken);
        }

        public Task<OrderResult> CreateOrderAsync(Order model, CancellationToken cancellationToken = default)
        {
            return WaitAndRetryForeverAsync(ct => _trader.CreateOrderAsync(model, ct), cancellationToken);
        }

        public Task<string> CreateUserDataStreamAsync(CancellationToken cancellationToken = default)
        {
            return WaitAndRetryForeverAsync(ct => _trader.CreateUserDataStreamAsync(ct), cancellationToken);
        }

        public Task<Ticker> Get24hTickerPriceChangeStatisticsAsync(string symbol, CancellationToken cancellationToken = default)
        {
            return WaitAndRetryForeverAsync(ct => _trader.Get24hTickerPriceChangeStatisticsAsync(symbol, ct), cancellationToken);
        }

        public Task<AccountInfo> GetAccountInfoAsync(GetAccountInfo model, CancellationToken cancellationToken = default)
        {
            return WaitAndRetryForeverAsync(ct => _trader.GetAccountInfoAsync(model, ct), cancellationToken);
        }

        public Task<ImmutableSortedTradeSet> GetAccountTradesAsync(string symbol, long? fromId, int? limit, CancellationToken cancellationToken = default)
        {
            return WaitAndRetryForeverAsync(ct => _trader.GetAccountTradesAsync(symbol, fromId, limit, ct), cancellationToken);
        }

        public Task<ImmutableSortedOrderSet> GetAllOrdersAsync(GetAllOrders model, CancellationToken cancellationToken = default)
        {
            return WaitAndRetryForeverAsync(ct => _trader.GetAllOrdersAsync(model, ct), cancellationToken);
        }

        public IReadOnlyCollection<FlexibleProduct> GetCachedFlexibleProductsByAsset(string asset)
        {
            return _trader.GetCachedFlexibleProductsByAsset(asset);
        }

        public Task<ExchangeInfo> GetExchangeInfoAsync(CancellationToken cancellationToken = default)
        {
            return WaitAndRetryForeverAsync(ct => _trader.GetExchangeInfoAsync(ct), cancellationToken);
        }

        public Task<IReadOnlyCollection<FlexibleProduct>> GetFlexibleProductListAsync(FlexibleProductStatus status, FlexibleProductFeatured featured, long? current, long? size, CancellationToken cancellationToken = default)
        {
            return WaitAndRetryForeverAsync(ct => _trader.GetFlexibleProductListAsync(status, featured, current, size, ct), cancellationToken);
        }

        public Task<IReadOnlyCollection<FlexibleProductPosition>> GetFlexibleProductPositionAsync(string asset, CancellationToken cancellationToken = default)
        {
            return WaitAndRetryForeverAsync(ct => _trader.GetFlexibleProductPositionAsync(asset, ct), cancellationToken);
        }

        public Task<IReadOnlyCollection<Kline>> GetKlinesAsync(GetKlines model, CancellationToken cancellationToken = default)
        {
            return WaitAndRetryForeverAsync(ct => _trader.GetKlinesAsync(model, ct), cancellationToken);
        }

        public Task<ImmutableSortedOrderSet> GetOpenOrdersAsync(string symbol, CancellationToken cancellationToken = default)
        {
            return WaitAndRetryForeverAsync(ct => _trader.GetOpenOrdersAsync(symbol, ct), cancellationToken);
        }

        public Task<OrderQueryResult> GetOrderAsync(string symbol, long? orderId, string? originalClientOrderId, CancellationToken cancellationToken = default)
        {
            return WaitAndRetryForeverAsync(ct => _trader.GetOrderAsync(symbol, orderId, originalClientOrderId, ct), cancellationToken);
        }

        public Task<SymbolPriceTicker> GetSymbolPriceTickerAsync(string symbol, CancellationToken cancellationToken = default)
        {
            return WaitAndRetryForeverAsync(ct => _trader.GetSymbolPriceTickerAsync(symbol, ct), cancellationToken);
        }

        public Task PingUserDataStreamAsync(string listenKey, CancellationToken cancellationToken = default)
        {
            return WaitAndRetryForeverAsync(ct => _trader.PingUserDataStreamAsync(listenKey, ct), cancellationToken);
        }

        public Task RedeemFlexibleProductAsync(string productId, decimal amount, FlexibleProductRedemptionType type, CancellationToken cancellationToken = default)
        {
            return WaitAndRetryForeverAsync(ct => _trader.RedeemFlexibleProductAsync(productId, amount, type, ct), cancellationToken);
        }

        public Task<LeftDailyRedemptionQuotaOnFlexibleProduct?> TryGetLeftDailyRedemptionQuotaOnFlexibleProductAsync(string productId, FlexibleProductRedemptionType type, CancellationToken cancellationToken = default)
        {
            return WaitAndRetryForeverAsync(ct => _trader.TryGetLeftDailyRedemptionQuotaOnFlexibleProductAsync(productId, type, ct), cancellationToken);
        }
    }
}