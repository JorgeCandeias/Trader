using AutoMapper;
using FastMember;
using Microsoft.Extensions.ObjectPool;
using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Outcompute.Trader.Trading.Binance
{
    internal class BinanceApiClient
    {
        private readonly HttpClient _client;
        private readonly IMapper _mapper;
        private readonly ObjectPool<StringBuilder> _pool;

        public BinanceApiClient(HttpClient client, IMapper mapper, ObjectPool<StringBuilder> pool)
        {
            _client = client;
            _mapper = mapper;
            _pool = pool;
        }

        private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        #region General Endpoints

        public async Task<bool> PingAsync(CancellationToken cancellationToken = default)
        {
            var response = await _client
                .GetAsync(
                    new Uri("/api/v3/ping", UriKind.Relative),
                    cancellationToken)
                .ConfigureAwait(false);

            return response.IsSuccessStatusCode;
        }

        public async Task<DateTime> GetTimeAsync(CancellationToken cancellationToken = default)
        {
            var result = await _client
                .GetFromJsonAsync<ApiServerTime>(
                    new Uri("/api/v3/time", UriKind.Relative),
                    cancellationToken)
                .ConfigureAwait(false);

            return _mapper.Map<DateTime>(result);
        }

        public async Task<ApiExchangeInfo> GetExchangeInfoAsync(CancellationToken cancellationToken = default)
        {
            return await _client
                .GetFromJsonAsync<ApiExchangeInfo>(
                    new Uri("/api/v3/exchangeInfo", UriKind.Relative),
                    cancellationToken)
                .ConfigureAwait(false) ?? throw new BinanceUnknownResponseException();
        }

        #endregion General Endpoints

        #region Market Data Endpoints

        public async Task<OrderBook> GetOrderBookAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var result = await _client
                .GetFromJsonAsync<ApiOrderBook>(
                    new Uri($"/api/v3/depth?symbol={HttpUtility.UrlEncode(symbol)}", UriKind.Relative),
                    cancellationToken)
                .ConfigureAwait(false);

            return _mapper.Map<OrderBook>(result);
        }

        public async Task<IEnumerable<Trade>> GetRecentTradesAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var result = await _client
                .GetFromJsonAsync<ApiTrade[]>(
                    new Uri($"/api/v3/trades?symbol={HttpUtility.UrlEncode(symbol)}", UriKind.Relative),
                    cancellationToken)
                .ConfigureAwait(false);

            return _mapper.Map<IEnumerable<Trade>>(result);
        }

        public async Task<IEnumerable<Trade>> GetHistoricalTradesAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var result = await _client
                .GetFromJsonAsync<ApiTrade[]>(
                    new Uri($"/api/v3/historicalTrades?symbol={HttpUtility.UrlEncode(symbol)}", UriKind.Relative),
                    cancellationToken)
                .ConfigureAwait(false);

            return _mapper.Map<IEnumerable<Trade>>(result);
        }

        public async Task<ApiTicker> Get24hTickerPriceChangeStatisticsAsync(string symbol, CancellationToken cancellationToken = default)
        {
            _ = symbol ?? throw new ArgumentNullException(nameof(symbol));

            return await _client
                .GetFromJsonAsync<ApiTicker>(
                    new Uri($"/api/v3/ticker/24hr?symbol={HttpUtility.UrlEncode(symbol)}", UriKind.Relative),
                    cancellationToken)
                .ConfigureAwait(false) ?? throw new BinanceUnknownResponseException();
        }

        public async Task<IReadOnlyCollection<ApiTicker>> Get24hTickerPriceChangeStatisticsAsync(CancellationToken cancellationToken = default)
        {
            return await _client
                .GetFromJsonAsync<ApiTicker[]>(
                    new Uri($"/api/v3/ticker/24hr", UriKind.Relative),
                    cancellationToken)
                .ConfigureAwait(false) ?? throw new BinanceUnknownResponseException();
        }

        public async Task<ApiSymbolPriceTicker> GetSymbolPriceTickerAsync(string symbol, CancellationToken cancellationToken = default)
        {
            _ = symbol ?? throw new ArgumentNullException(nameof(symbol));

            BinanceApiContext.SkipSigning = true;

            return await _client
                .GetFromJsonAsync<ApiSymbolPriceTicker>(
                    new Uri($"/api/v3/ticker/price?symbol={HttpUtility.UrlEncode(symbol)}", UriKind.Relative),
                    cancellationToken)
                .ConfigureAwait(false) ?? throw new BinanceUnknownResponseException();
        }

        public async Task<IEnumerable<SymbolPriceTicker>> GetSymbolPriceTickersAsync(CancellationToken cancellationToken = default)
        {
            var result = await _client
                .GetFromJsonAsync<IEnumerable<ApiSymbolPriceTicker>>(
                    new Uri($"/api/v3/ticker/price", UriKind.Relative),
                    cancellationToken)
                .ConfigureAwait(false);

            return _mapper.Map<IEnumerable<SymbolPriceTicker>>(result);
        }

        public async Task<SymbolOrderBookTicker> GetSymbolOrderBookTickerAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var result = await _client
                .GetFromJsonAsync<ApiSymbolOrderBookTicker>(
                    new Uri($"/api/v3/ticker/bookTicker?symbol={HttpUtility.UrlEncode(symbol)}", UriKind.Relative),
                    cancellationToken)
                .ConfigureAwait(false);

            return _mapper.Map<SymbolOrderBookTicker>(result);
        }

        public async Task<IEnumerable<SymbolOrderBookTicker>> GetSymbolOrderBookTickersAsync(CancellationToken cancellationToken = default)
        {
            var result = await _client
                .GetFromJsonAsync<IEnumerable<ApiSymbolOrderBookTicker>>(
                    new Uri($"/api/v3/ticker/bookTicker", UriKind.Relative),
                    cancellationToken)
                .ConfigureAwait(false);

            return _mapper.Map<IEnumerable<SymbolOrderBookTicker>>(result);
        }

        #endregion Market Data Endpoints

        #region Account Endpoints

        /// <summary>
        /// Creates the specified order.
        /// </summary>
        public async Task<CreateOrderResponse> CreateOrderAsync(CreateOrderRequest model, CancellationToken cancellationToken = default)
        {
            _ = model ?? throw new ArgumentNullException(nameof(model));
            _ = model.Symbol ?? throw new ArgumentException($"{nameof(OrderQuery.Symbol)} is required");

            var response = await _client
                .PostAsync(
                    Combine(new Uri("/api/v3/order", UriKind.Relative), model),
                    EmptyHttpContent.Instance,
                    cancellationToken)
                .ConfigureAwait(false);

            return await response.Content
                .ReadFromJsonAsync<CreateOrderResponse>(_jsonOptions, cancellationToken)
                .ConfigureAwait(false) ?? throw new BinanceUnknownResponseException();
        }

        /// <summary>
        /// Gets the status of the specified order.
        /// </summary>
        public async Task<GetOrderResponse> GetOrderAsync(GetOrderRequest model, CancellationToken cancellationToken = default)
        {
            _ = model ?? throw new ArgumentNullException(nameof(model));
            _ = model.Symbol ?? throw new ArgumentException($"{nameof(OrderQuery.Symbol)} is required");

            return await _client
                .GetFromJsonAsync<GetOrderResponse>(
                    Combine(new Uri("/api/v3/order", UriKind.Relative), model),
                    _jsonOptions,
                    cancellationToken)
                .ConfigureAwait(false) ?? throw new BinanceUnknownResponseException();
        }

        /// <summary>
        /// Cancels the specified order.
        /// </summary>
        public async Task<CancelOrderResponse> CancelOrderAsync(CancelOrderRequest model, CancellationToken cancellationToken = default)
        {
            _ = model ?? throw new ArgumentNullException(nameof(model));
            _ = model.Symbol ?? throw new ArgumentException($"{nameof(OrderQuery.Symbol)} is required");

            var output = await _client
                .DeleteAsync(
                    Combine(new Uri("/api/v3/order", UriKind.Relative), model),
                    cancellationToken)
                .ConfigureAwait(false);

            return await output.Content
                .ReadFromJsonAsync<CancelOrderResponse>(_jsonOptions, cancellationToken)
                .ConfigureAwait(false) ?? throw new BinanceUnknownResponseException();
        }

        /// <summary>
        /// Cancels all open orders.
        /// </summary>
        public async Task<IEnumerable<CancelAllOrdersResponse>> CancelAllOrdersAsync(CancelAllOrdersRequest model, CancellationToken cancellationToken = default)
        {
            var uri = Combine(new Uri("/api/v3/openOrders", UriKind.Relative), model);

            var response = await _client
                .DeleteAsync(uri, cancellationToken)
                .ConfigureAwait(false);

            return await response.Content
                .ReadFromJsonAsync<IEnumerable<CancelAllOrdersResponse>>(_jsonOptions, cancellationToken)
                .WithNullHandling()
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Gets all open orders.
        /// </summary>
        public async Task<IEnumerable<GetOrderResponse>> GetOpenOrdersAsync(GetOpenOrdersRequest model, CancellationToken cancellationToken = default)
        {
            _ = model ?? throw new ArgumentNullException(nameof(model));
            _ = model.Symbol ?? throw new ArgumentException($"{nameof(GetOpenOrders.Symbol)} is required");

            return await _client
                .GetFromJsonAsync<IEnumerable<GetOrderResponse>>(
                    Combine(new Uri("/api/v3/openOrders", UriKind.Relative), model),
                    _jsonOptions,
                    cancellationToken)
                .ConfigureAwait(false) ?? throw new BinanceUnknownResponseException();
        }

        /// <summary>
        /// Gets all orders.
        /// </summary>
        public async Task<IEnumerable<GetOrderResponse>> GetAllOrdersAsync(GetAllOrdersRequest model, CancellationToken cancellationToken = default)
        {
            _ = model ?? throw new ArgumentNullException(nameof(model));
            _ = model.Symbol ?? throw new ArgumentException($"{nameof(GetOpenOrders.Symbol)} is required");

            return await _client
                .GetFromJsonAsync<IEnumerable<GetOrderResponse>>(
                    Combine(new Uri("/api/v3/allOrders", UriKind.Relative), model),
                    _jsonOptions,
                    cancellationToken)
                .ConfigureAwait(false) ?? throw new BinanceUnknownResponseException();
        }

        /// <summary>
        /// Gets the account information.
        /// </summary>
        public async Task<AccountResponseModel> GetAccountInfoAsync(GetAccountInfoRequest model, CancellationToken cancellationToken = default)
        {
            _ = model ?? throw new ArgumentNullException(nameof(model));

            return await _client
                .GetFromJsonAsync<AccountResponseModel>(
                    Combine(new Uri("/api/v3/account", UriKind.Relative), model),
                    _jsonOptions,
                    cancellationToken)
                .ConfigureAwait(false) ?? throw new BinanceUnknownResponseException();
        }

        /// <summary>
        /// Gets the account trades.
        /// </summary>
        public async Task<IEnumerable<AccountTradesResponseModel>> GetAccountTradesAsync(AccountTradesRequestModel model, CancellationToken cancellationToken = default)
        {
            _ = model ?? throw new ArgumentNullException(nameof(model));
            _ = model.Symbol ?? throw new ArgumentException($"{nameof(AccountTradesRequestModel.Symbol)} is required");

            return await _client
                .GetFromJsonAsync<IEnumerable<AccountTradesResponseModel>>(
                    Combine(new Uri("/api/v3/myTrades", UriKind.Relative), model),
                    cancellationToken)
                .ConfigureAwait(false) ?? throw new BinanceUnknownResponseException();
        }

        public async Task<IEnumerable<KlineResponseModel>> GetKlinesAsync(KlineRequestModel model, CancellationToken cancellationToken = default)
        {
            _ = model ?? throw new ArgumentNullException(nameof(model));

            var response = await _client
                .GetFromJsonAsync<IEnumerable<JsonElement[]>>(Combine(new Uri("/api/v3/klines", UriKind.Relative), model), cancellationToken)
                .ConfigureAwait(false);

            return _mapper.Map<IEnumerable<KlineResponseModel>>(response);
        }

        /// <summary>
        /// Starts a new user data stream and returns the listen key for it.
        /// </summary>
        public async Task<ListenKeyResponseModel> CreateUserDataStreamAsync(CancellationToken cancellationToken = default)
        {
            var response = await _client
                .PostAsync(
                    new Uri("/api/v3/userDataStream", UriKind.Relative),
                    EmptyHttpContent.Instance,
                    cancellationToken)
                .ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            return await response.Content
                .ReadFromJsonAsync<ListenKeyResponseModel>(_jsonOptions, cancellationToken)
                .ConfigureAwait(false) ?? throw new BinanceUnknownResponseException();
        }

        public async Task PingUserDataStreamAsync(ListenKeyRequestModel model, CancellationToken cancellationToken = default)
        {
            _ = model ?? throw new ArgumentNullException(nameof(model));

            await _client
                .PutAsync(
                    Combine(new Uri("/api/v3/userDataStream", UriKind.Relative), model),
                    EmptyHttpContent.Instance,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task CloseUserDataStreamAsync(ListenKeyRequestModel model, CancellationToken cancellationToken = default)
        {
            _ = model ?? throw new ArgumentNullException(nameof(model));

            await _client
                .DeleteAsync(
                    Combine(new Uri("/api/v3/userDataStream", UriKind.Relative), model),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        #endregion Account Endpoints

        #region Savings Endpoints

        public async Task<LeftDailyRedemptionQuotaOnFlexibleProductResponseModel?> GetLeftDailyRedemptionQuotaOnFlexibleProductAsync(LeftDailyRedemptionQuotaOnFlexibleProductRequestModel model, CancellationToken cancellationToken = default)
        {
            _ = model ?? throw new ArgumentNullException(nameof(model));

            try
            {
                return await _client
                    .GetFromJsonAsync<LeftDailyRedemptionQuotaOnFlexibleProductResponseModel>(Combine(new Uri("/sapi/v1/lending/daily/userRedemptionQuota", UriKind.Relative), model), cancellationToken)
                    .ConfigureAwait(false) ?? throw new BinanceUnknownResponseException();
            }
            catch (BinanceCodeException ex) when (ex.BinanceCode == -6001)
            {
                // handle "daily product does not exist" response
                return null;
            }
        }

        public async Task<IEnumerable<FlexibleProductPositionResponseModel>> GetFlexibleProductPositionAsync(FlexibleProductPositionRequestModel model, CancellationToken cancellationToken = default)
        {
            _ = model ?? throw new ArgumentNullException(nameof(model));

            return await _client
                .GetFromJsonAsync<IEnumerable<FlexibleProductPositionResponseModel>>(Combine(new Uri("/sapi/v1/lending/daily/token/position", UriKind.Relative), model), cancellationToken)
                .ConfigureAwait(false) ?? throw new BinanceUnknownResponseException();
        }

        public async Task RedeemFlexibleProductAsync(FlexibleProductRedemptionRequestModel model, CancellationToken cancellationToken = default)
        {
            _ = model ?? throw new ArgumentNullException(nameof(model));

            var response = await _client
                .PostAsync(Combine(new Uri("/sapi/v1/lending/daily/redeem", UriKind.Relative), model), EmptyHttpContent.Instance, cancellationToken)
                .ConfigureAwait(false);

            response.EnsureSuccessStatusCode();
        }

        private readonly Uri _getFlexibleProductListUri = new("/sapi/v1/lending/daily/product/list", UriKind.Relative);

        public async Task<IEnumerable<FlexibleProductResponseModel>> GetFlexibleProductListAsync(FlexibleProductRequestModel model, CancellationToken cancellationToken = default)
        {
            _ = model ?? throw new ArgumentNullException(nameof(model));

            var uri = Combine(_getFlexibleProductListUri, model);

            return await _client
                .GetFromJsonAsync<IEnumerable<FlexibleProductResponseModel>>(uri, cancellationToken)
                .ConfigureAwait(false) ?? throw new BinanceUnknownResponseException();
        }

        #endregion Savings Endpoints

        #region Swap Endpoints

        public async Task<IEnumerable<SwapPoolResponseModel>> GetSwapPoolsAsync(CancellationToken cancellationToken = default)
        {
            var uri = new Uri("/sapi/v1/bswap/pools", UriKind.Relative);

            return await _client
                .GetFromJsonAsync<IEnumerable<SwapPoolResponseModel>>(uri, cancellationToken)
                .WithNullHandling();
        }

        public Task<SwapPoolLiquidityResponseModel> GetSwapLiquidityAsync(SwapPoolLiquidityRequestModel model, CancellationToken cancellationToken = default)
        {
            if (model is null) throw new ArgumentNullException(nameof(model));

            var uri = Combine(new Uri("/sapi/v1/bswap/liquidity", UriKind.Relative), model);

            return _client
                .GetFromJsonAsync<SwapPoolLiquidityResponseModel>(uri, cancellationToken)
                .WithNullHandling();
        }

        public Task<IEnumerable<SwapPoolLiquidityResponseModel>> GetSwapLiquiditiesAsync(SwapPoolLiquidityRequestModel model, CancellationToken cancellationToken = default)
        {
            if (model is null) throw new ArgumentNullException(nameof(model));

            var uri = Combine(new Uri("/sapi/v1/bswap/liquidity", UriKind.Relative), model);

            return _client
                .GetFromJsonAsync<IEnumerable<SwapPoolLiquidityResponseModel>>(uri, cancellationToken)
                .WithNullHandling();
        }

        public async Task<SwapPoolAddLiquidityResponseModel> AddSwapLiquidityAsync(SwapPoolAddLiquidityRequestModel model, CancellationToken cancellationToken = default)
        {
            var uri = Combine(new Uri("/sapi/v1/bswap/liquidityAdd", UriKind.Relative), model);

            var result = await _client
                .PostAsync(uri, EmptyHttpContent.Instance, cancellationToken)
                .ConfigureAwait(false);

            result = result.EnsureSuccessStatusCode();

            return await result.Content
                .ReadFromJsonAsync<SwapPoolAddLiquidityResponseModel>(_jsonOptions, cancellationToken)
                .WithNullHandling()
                .ConfigureAwait(false);
        }

        public async Task<SwapPoolRemoveLiquidityResponse> RemoveSwapLiquidityAsync(SwapPoolRemoveLiquidityRequest model, CancellationToken cancellationToken = default)
        {
            var uri = Combine(new Uri("/sapi/v1/bswap/liquidityRemove", UriKind.Relative), model);

            var result = await _client
                .PostAsync(uri, EmptyHttpContent.Instance, cancellationToken)
                .ConfigureAwait(false);

            result = result.EnsureSuccessStatusCode();

            return await result.Content
                .ReadFromJsonAsync<SwapPoolRemoveLiquidityResponse>(_jsonOptions, cancellationToken)
                .WithNullHandling()
                .ConfigureAwait(false);
        }

        #endregion Swap Endpoints

        #region Helpers

        /// <summary>
        /// Caches type data at zero lookup cost.
        /// </summary>
        [SuppressMessage("Minor Code Smell", "S3260:Non-derived \"private\" classes and records should be \"sealed\"", Justification = "Type Cache Pattern")]
        private static class TypeCache<T>
        {
            public static TypeAccessor TypeAccessor { get; } = TypeAccessor.Create(typeof(T));

            public static ImmutableArray<(string Name, string LowerName)> Names { get; } = TypeCache<T>.TypeAccessor
                .GetMembers()
                .Select(x => (x.Name, char.ToLowerInvariant(x.Name[0]) + x.Name[1..]))
                .ToImmutableArray();
        }

        private Uri Combine<T>(Uri requestUri, T data)
        {
            var builder = _pool.Get();

            builder.Append('?');

            try
            {
                var next = false;

                foreach (var (name, lowerName) in TypeCache<T>.Names)
                {
                    var value = TypeCache<T>.TypeAccessor[data, name];

                    if (value is not null)
                    {
                        if (next)
                        {
                            builder.Append('&');
                        }

                        builder.Append(lowerName).Append('=').Append(value);

                        next = true;
                    }
                }

                return new Uri(requestUri.ToString() + builder.ToString(), requestUri.IsAbsoluteUri ? UriKind.Absolute : UriKind.Relative);
            }
            finally
            {
                _pool.Return(builder);
            }
        }

        private async Task<TResult> DeleteAsync<TRequest, TResponse, TResult>(Uri requestUri, object data, CancellationToken cancellationToken = default)
        {
            var request = _mapper.Map<TRequest>(data);

            var response = await _client
                .DeleteAsync(Combine(requestUri, request), cancellationToken)
                .ConfigureAwait(false);

            var typed = await response.Content
                .ReadFromJsonAsync<TResponse>(_jsonOptions, cancellationToken)
                .ConfigureAwait(false);

            return _mapper.Map<TResult>(typed);
        }

        #endregion Helpers
    }

    internal static class BinanceApiClientExtensions
    {
        public static async Task<T> WithNullHandling<T>(this Task<T?> task)
        {
            var result = await task.ConfigureAwait(false);
            return result ?? throw new BinanceUnknownResponseException();
        }
    }
}