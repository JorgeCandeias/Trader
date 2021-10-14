using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Binance
{
    internal class BinanceTradingService : ITradingService, IHostedService
    {
        private readonly ILogger _logger;
        private readonly BinanceApiClient _client;
        private readonly BinanceUsageContext _usage;
        private readonly IMapper _mapper;
        private readonly ISystemClock _clock;
        private readonly IServiceProvider _provider;

        public BinanceTradingService(ILogger<BinanceTradingService> logger, BinanceApiClient client, BinanceUsageContext usage, IMapper mapper, ISystemClock clock, IServiceProvider provider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _usage = usage ?? throw new ArgumentNullException(nameof(usage));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        private static string Name => nameof(BinanceTradingService);

        private ImmutableDictionary<string, ImmutableList<FlexibleProduct>> _flexibleProducts = ImmutableDictionary<string, ImmutableList<FlexibleProduct>>.Empty;

        public ITradingService WithBackoff()
        {
            // we must use the provider here in order to avoid recursiveness in service resolution
            return _provider.GetRequiredService<BinanceTradingServiceWithBackoff>();
        }

        public async Task<ExchangeInfo> GetExchangeInfoAsync(CancellationToken cancellationToken = default)
        {
            var output = await _client
                .GetExchangeInfoAsync(cancellationToken)
                .ConfigureAwait(false);

            return _mapper.Map<ExchangeInfo>(output);
        }

        public async Task<SymbolPriceTicker> GetSymbolPriceTickerAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var output = await _client
                .GetSymbolPriceTickerAsync(symbol, cancellationToken)
                .ConfigureAwait(false);

            return _mapper.Map<SymbolPriceTicker>(output);
        }

        public async Task<ImmutableSortedTradeSet> GetAccountTradesAsync(string symbol, long? fromId, int? limit, CancellationToken cancellationToken = default)
        {
            var model = new GetAccountTrades(symbol, null, null, fromId, limit, null, _clock.UtcNow);
            var input = _mapper.Map<AccountTradesRequestModel>(model);

            var output = await _client
                .GetAccountTradesAsync(input, cancellationToken)
                .ConfigureAwait(false);

            return _mapper.Map<ImmutableSortedTradeSet>(output);
        }

        public async Task<ImmutableSortedOrderSet> GetOpenOrdersAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var model = new GetOpenOrders(symbol, null, _clock.UtcNow);
            var input = _mapper.Map<GetOpenOrdersRequestModel>(model);

            var output = await _client
                .GetOpenOrdersAsync(input, cancellationToken)
                .ConfigureAwait(false);

            return _mapper.Map<ImmutableSortedOrderSet>(output);
        }

        public async Task<OrderQueryResult> GetOrderAsync(string symbol, long? orderId, string? originalClientOrderId, CancellationToken cancellationToken = default)
        {
            var model = new OrderQuery(symbol, orderId, originalClientOrderId, null, _clock.UtcNow);
            var input = _mapper.Map<GetOrderRequestModel>(model);

            var output = await _client
                .GetOrderAsync(input, cancellationToken)
                .ConfigureAwait(false);

            return _mapper.Map<OrderQueryResult>(output);
        }

        public async Task<ImmutableSortedOrderSet> GetAllOrdersAsync(string symbol, long? orderId, int? limit, CancellationToken cancellationToken = default)
        {
            var model = new GetAllOrders(symbol, orderId, null, null, limit, null, _clock.UtcNow);
            var input = _mapper.Map<GetAllOrdersRequestModel>(model);

            var output = await _client
                .GetAllOrdersAsync(input, cancellationToken)
                .ConfigureAwait(false);

            return _mapper.Map<ImmutableSortedOrderSet>(output);
        }

        public async Task<OrderResult> CreateOrderAsync(string symbol, OrderSide side, OrderType type, TimeInForce? timeInForce, decimal? quantity, decimal? quoteOrderQuantity, decimal? price, string? newClientOrderId, decimal? stopPrice, decimal? icebergQuantity, CancellationToken cancellationToken = default)
        {
            var model = new Order(symbol, side, type, timeInForce, quantity, quoteOrderQuantity, price, newClientOrderId, stopPrice, icebergQuantity, NewOrderResponseType.Full, null, _clock.UtcNow);
            var input = _mapper.Map<NewOrderRequestModel>(model);

            var output = await _client
                .CreateOrderAsync(input, cancellationToken)
                .ConfigureAwait(false);

            return _mapper.Map<OrderResult>(output);
        }

        public async Task<CancelStandardOrderResult> CancelOrderAsync(string symbol, long orderId, CancellationToken cancellationToken = default)
        {
            var model = new CancelStandardOrder(symbol, orderId, null, null, null, _clock.UtcNow);
            var input = _mapper.Map<CancelOrderRequestModel>(model);

            var output = await _client
                .CancelOrderAsync(input, cancellationToken)
                .ConfigureAwait(false);

            return _mapper.Map<CancelStandardOrderResult>(output);
        }

        public async Task<AccountInfo> GetAccountInfoAsync(CancellationToken cancellationToken = default)
        {
            var model = new GetAccountInfo(null, _clock.UtcNow);
            var input = _mapper.Map<AccountRequestModel>(model);

            var output = await _client
                .GetAccountInfoAsync(input, cancellationToken)
                .ConfigureAwait(false);

            return _mapper.Map<AccountInfo>(output);
        }

        public async Task<Ticker> Get24hTickerPriceChangeStatisticsAsync(string symbol, CancellationToken cancellationToken = default)
        {
            _ = symbol ?? throw new ArgumentNullException(nameof(symbol));

            BinanceApiContext.SkipSigning = true;

            var output = await _client
                .Get24hTickerPriceChangeStatisticsAsync(symbol, cancellationToken)
                .ConfigureAwait(false);

            return _mapper.Map<Ticker>(output);
        }

        public async Task<IReadOnlyCollection<Kline>> GetKlinesAsync(string symbol, KlineInterval interval, DateTime startTime, DateTime endTime, int limit, CancellationToken cancellationToken = default)
        {
            var model = new GetKlines(symbol, interval, startTime, endTime, limit);
            var input = _mapper.Map<KlineRequestModel>(model);

            BinanceApiContext.SkipSigning = true;

            var output = await _client
                .GetKlinesAsync(input, cancellationToken)
                .ConfigureAwait(false);

            return _mapper.Map<IReadOnlyCollection<Kline>>(output, options =>
            {
                options.Items[nameof(Kline.Symbol)] = model.Symbol;
                options.Items[nameof(Kline.Interval)] = model.Interval;
            });
        }

        public async Task<IReadOnlyCollection<FlexibleProductPosition>> GetFlexibleProductPositionAsync(
            string asset,
            CancellationToken cancellationToken = default)
        {
            var model = new GetFlexibleProductPosition(asset, null, _clock.UtcNow);

            var input = _mapper.Map<FlexibleProductPositionRequestModel>(model);

            var output = await _client
                .GetFlexibleProductPositionAsync(input, cancellationToken)
                .ConfigureAwait(false);

            return _mapper.Map<IReadOnlyCollection<FlexibleProductPosition>>(output);
        }

        public async Task<LeftDailyRedemptionQuotaOnFlexibleProduct?> TryGetLeftDailyRedemptionQuotaOnFlexibleProductAsync(
            string productId,
            FlexibleProductRedemptionType type,
            CancellationToken cancellationToken = default)
        {
            var model = new GetLeftDailyRedemptionQuotaOnFlexibleProduct(productId, type, null, _clock.UtcNow);

            var input = _mapper.Map<LeftDailyRedemptionQuotaOnFlexibleProductRequestModel>(model);

            var output = await _client
                .GetLeftDailyRedemptionQuotaOnFlexibleProductAsync(input, cancellationToken)
                .ConfigureAwait(false);

            return _mapper.Map<LeftDailyRedemptionQuotaOnFlexibleProduct>(output);
        }

        public async Task RedeemFlexibleProductAsync(
            string productId,
            decimal amount,
            FlexibleProductRedemptionType type,
            CancellationToken cancellationToken = default)
        {
            var model = new RedeemFlexibleProduct(productId, amount, type, null, _clock.UtcNow);

            var input = _mapper.Map<FlexibleProductRedemptionRequestModel>(model);

            await _client
                .RedeemFlexibleProductAsync(input, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<FlexibleProduct>> GetFlexibleProductListAsync(
            FlexibleProductStatus status,
            FlexibleProductFeatured featured,
            long? current,
            long? size,
            CancellationToken cancellationToken = default)
        {
            var model = new GetFlexibleProduct(status, featured, current, size, null, _clock.UtcNow);

            var input = _mapper.Map<FlexibleProductRequestModel>(model);

            var output = await _client
                .GetFlexibleProductListAsync(input, cancellationToken)
                .ConfigureAwait(false);

            return _mapper.Map<IReadOnlyCollection<FlexibleProduct>>(output);
        }

        public IReadOnlyCollection<FlexibleProduct> GetCachedFlexibleProductsByAsset(string asset)
        {
            if (_flexibleProducts.TryGetValue(asset, out var value))
            {
                return value;
            }

            return ImmutableList<FlexibleProduct>.Empty;
        }

        public async Task<string> CreateUserDataStreamAsync(CancellationToken cancellationToken = default)
        {
            var output = await _client
                .CreateUserDataStreamAsync(cancellationToken)
                .ConfigureAwait(false);

            return output.ListenKey;
        }

        public async Task PingUserDataStreamAsync(string listenKey, CancellationToken cancellationToken = default)
        {
            _ = listenKey ?? throw new ArgumentNullException(nameof(listenKey));

            BinanceApiContext.SkipSigning = true;

            await _client
                .PingUserDataStreamAsync(new ListenKeyRequestModel(listenKey), cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task CloseUserDataStreamAsync(string listenKey, CancellationToken cancellationToken = default)
        {
            _ = listenKey ?? throw new ArgumentNullException(nameof(listenKey));

            BinanceApiContext.SkipSigning = true;

            await _client
                .CloseUserDataStreamAsync(new ListenKeyRequestModel(listenKey), cancellationToken)
                .ConfigureAwait(false);
        }

        // todo: refactor this into a grain and remove this service as a hosted service
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await SyncLimitsAsync(cancellationToken).ConfigureAwait(false);
            await SyncFlexibleProductsAsync(cancellationToken).ConfigureAwait(false);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        #region Helpers

        private async Task SyncLimitsAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("{Name} querying exchange rate limits...", Name);

            // get the exchange request limits
            var result = await _client
                .GetExchangeInfoAsync(cancellationToken)
                .ConfigureAwait(false);

            var info = _mapper.Map<ExchangeInfo>(result);

            // keep the request weight limits
            foreach (var limit in info.RateLimits)
            {
                if (limit.Limit == 0)
                {
                    throw new BinanceException("Received unexpected rate limit of zero from the exchange");
                }

                _usage.SetLimit(limit.Type, limit.TimeSpan, limit.Limit);
            }
        }

        private async Task SyncFlexibleProductsAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("{Name} querying flexible products...", Name);

            var page = 0;
            var list = new List<FlexibleProduct>();

            while (true)
            {
                var result = await GetFlexibleProductListAsync(FlexibleProductStatus.All, FlexibleProductFeatured.All, ++page, 100, cancellationToken)
                    .ConfigureAwait(false);

                // stop if there are no more items to get
                if (result.Count == 0) break;

                _logger.LogInformation("{Name} queried a page with {Count} flexible products...", Name, result.Count);

                // otherwise keep the items
                list.AddRange(result);
            }

            _logger.LogInformation("{Name} queried a total of {Count} flexible products", Name, list.Count);

            // keep the items in a safe state for concurrent querying
            _flexibleProducts = list.GroupBy(x => x.Asset).ToImmutableDictionary(x => x.Key, x => x.ToImmutableList());
        }

        #endregion Helpers
    }
}