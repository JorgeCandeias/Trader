using AutoMapper;
using FastMember;
using Microsoft.Extensions.ObjectPool;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Outcompute.Trader.Models;

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
                .GetFromJsonAsync<ServerTimeModel>(
                    new Uri("/api/v3/time", UriKind.Relative),
                    cancellationToken)
                .ConfigureAwait(false);

            return _mapper.Map<DateTime>(result);
        }

        public async Task<ExchangeInfoModel> GetExchangeInfoAsync(CancellationToken cancellationToken = default)
        {
            return await _client
                .GetFromJsonAsync<ExchangeInfoModel>(
                    new Uri("/api/v3/exchangeInfo", UriKind.Relative),
                    cancellationToken)
                .ConfigureAwait(false) ?? throw new BinanceUnknownResponseException();
        }

        #endregion General Endpoints

        #region Market Data Endpoints

        public async Task<OrderBook> GetOrderBookAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var result = await _client
                .GetFromJsonAsync<OrderBookModel>(
                    new Uri($"/api/v3/depth?symbol={HttpUtility.UrlEncode(symbol)}", UriKind.Relative),
                    cancellationToken)
                .ConfigureAwait(false);

            return _mapper.Map<OrderBook>(result);
        }

        public async Task<IEnumerable<Trade>> GetRecentTradesAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var result = await _client
                .GetFromJsonAsync<TradeModel[]>(
                    new Uri($"/api/v3/trades?symbol={HttpUtility.UrlEncode(symbol)}", UriKind.Relative),
                    cancellationToken)
                .ConfigureAwait(false);

            return _mapper.Map<IEnumerable<Trade>>(result);
        }

        public async Task<IEnumerable<Trade>> GetHistoricalTradesAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var result = await _client
                .GetFromJsonAsync<TradeModel[]>(
                    new Uri($"/api/v3/historicalTrades?symbol={HttpUtility.UrlEncode(symbol)}", UriKind.Relative),
                    cancellationToken)
                .ConfigureAwait(false);

            return _mapper.Map<IEnumerable<Trade>>(result);
        }

        public async Task<IEnumerable<AggTrade>> GetAggTradesAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var result = await _client
                .GetFromJsonAsync<IDictionary<string, JsonElement>[]>(
                    new Uri($"/api/v3/aggTrades?symbol={HttpUtility.UrlEncode(symbol)}", UriKind.Relative), _jsonOptions,
                    cancellationToken)
                .ConfigureAwait(false);

            return _mapper.Map<IEnumerable<AggTrade>>(result);
        }

        public async Task<AvgPrice> GetAvgPriceAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var result = await _client
                .GetFromJsonAsync<AvgPriceModel>(
                    new Uri($"/api/v3/avgPrice?symbol={HttpUtility.UrlEncode(symbol)}", UriKind.Relative),
                    cancellationToken)
                .ConfigureAwait(false);

            return _mapper.Map<AvgPrice>(result);
        }

        public async Task<TickerModel> Get24hTickerPriceChangeStatisticsAsync(string symbol, CancellationToken cancellationToken = default)
        {
            _ = symbol ?? throw new ArgumentNullException(nameof(symbol));

            return await _client
                .GetFromJsonAsync<TickerModel>(
                    new Uri($"/api/v3/ticker/24hr?symbol={HttpUtility.UrlEncode(symbol)}", UriKind.Relative),
                    cancellationToken)
                .ConfigureAwait(false) ?? throw new BinanceUnknownResponseException();
        }

        public async Task<SymbolPriceTickerModel> GetSymbolPriceTickerAsync(string symbol, CancellationToken cancellationToken = default)
        {
            _ = symbol ?? throw new ArgumentNullException(nameof(symbol));

            BinanceApiContext.SkipSigning = true;

            return await _client
                .GetFromJsonAsync<SymbolPriceTickerModel>(
                    new Uri($"/api/v3/ticker/price?symbol={HttpUtility.UrlEncode(symbol)}", UriKind.Relative),
                    cancellationToken)
                .ConfigureAwait(false) ?? throw new BinanceUnknownResponseException();
        }

        public async Task<ImmutableList<SymbolPriceTicker>> GetSymbolPriceTickersAsync(CancellationToken cancellationToken = default)
        {
            var result = await _client
                .GetFromJsonAsync<IEnumerable<SymbolPriceTickerModel>>(
                    new Uri($"/api/v3/ticker/price", UriKind.Relative),
                    cancellationToken)
                .ConfigureAwait(false);

            return _mapper.Map<ImmutableList<SymbolPriceTicker>>(result);
        }

        public async Task<SymbolOrderBookTicker> GetSymbolOrderBookTickerAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var result = await _client
                .GetFromJsonAsync<SymbolOrderBookTickerModel>(
                    new Uri($"/api/v3/ticker/bookTicker?symbol={HttpUtility.UrlEncode(symbol)}", UriKind.Relative),
                    cancellationToken)
                .ConfigureAwait(false);

            return _mapper.Map<SymbolOrderBookTicker>(result);
        }

        public async Task<IEnumerable<SymbolOrderBookTicker>> GetSymbolOrderBookTickersAsync(CancellationToken cancellationToken = default)
        {
            var result = await _client
                .GetFromJsonAsync<IEnumerable<SymbolOrderBookTickerModel>>(
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
        public async Task<NewOrderResponseModel> CreateOrderAsync(NewOrderRequestModel model, CancellationToken cancellationToken = default)
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
                .ReadFromJsonAsync<NewOrderResponseModel>(_jsonOptions, cancellationToken)
                .ConfigureAwait(false) ?? throw new BinanceUnknownResponseException();
        }

        /// <summary>
        /// Gets the status of the specified order.
        /// </summary>
        public async Task<GetOrderResponseModel> GetOrderAsync(GetOrderRequestModel model, CancellationToken cancellationToken = default)
        {
            _ = model ?? throw new ArgumentNullException(nameof(model));
            _ = model.Symbol ?? throw new ArgumentException($"{nameof(OrderQuery.Symbol)} is required");

            return await _client
                .GetFromJsonAsync<GetOrderResponseModel>(
                    Combine(new Uri("/api/v3/order", UriKind.Relative), model),
                    _jsonOptions,
                    cancellationToken)
                .ConfigureAwait(false) ?? throw new BinanceUnknownResponseException();
        }

        /// <summary>
        /// Cancels the specified order.
        /// </summary>
        public async Task<CancelOrderResponseModel> CancelOrderAsync(CancelOrderRequestModel model, CancellationToken cancellationToken = default)
        {
            _ = model ?? throw new ArgumentNullException(nameof(model));
            _ = model.Symbol ?? throw new ArgumentException($"{nameof(OrderQuery.Symbol)} is required");

            var output = await _client
                .DeleteAsync(
                    Combine(new Uri("/api/v3/order", UriKind.Relative), model),
                    cancellationToken)
                .ConfigureAwait(false);

            return await output.Content
                .ReadFromJsonAsync<CancelOrderResponseModel>(_jsonOptions, cancellationToken)
                .ConfigureAwait(false) ?? throw new BinanceUnknownResponseException();
        }

        /// <summary>
        /// Cancels all open orders.
        /// </summary>
        public Task<ImmutableList<CancelOrderResult>> CancelAllOrdersAsync(CancelAllOrders cancellation, CancellationToken cancellationToken = default)
        {
            _ = cancellation ?? throw new ArgumentNullException(nameof(cancellation));
            _ = cancellation.Symbol ?? throw new ArgumentException($"{nameof(OrderQuery.Symbol)} is required");

            return DeleteAsync<CancelAllOrdersRequestModel, IEnumerable<CancelAllOrdersResponseModel>, ImmutableList<CancelOrderResult>>(
                new Uri("/api/v3/openOrders", UriKind.Relative),
                cancellation,
                cancellationToken);
        }

        /// <summary>
        /// Gets all open orders.
        /// </summary>
        public async Task<IEnumerable<GetOrderResponseModel>> GetOpenOrdersAsync(GetOpenOrdersRequestModel model, CancellationToken cancellationToken = default)
        {
            _ = model ?? throw new ArgumentNullException(nameof(model));
            _ = model.Symbol ?? throw new ArgumentException($"{nameof(GetOpenOrders.Symbol)} is required");

            return await _client
                .GetFromJsonAsync<IEnumerable<GetOrderResponseModel>>(
                    Combine(new Uri("/api/v3/openOrders", UriKind.Relative), model),
                    _jsonOptions,
                    cancellationToken)
                .ConfigureAwait(false) ?? throw new BinanceUnknownResponseException();
        }

        /// <summary>
        /// Gets all orders.
        /// </summary>
        public async Task<IEnumerable<GetOrderResponseModel>> GetAllOrdersAsync(GetAllOrdersRequestModel model, CancellationToken cancellationToken = default)
        {
            _ = model ?? throw new ArgumentNullException(nameof(model));
            _ = model.Symbol ?? throw new ArgumentException($"{nameof(GetOpenOrders.Symbol)} is required");

            return await _client
                .GetFromJsonAsync<IEnumerable<GetOrderResponseModel>>(
                    Combine(new Uri("/api/v3/allOrders", UriKind.Relative), model),
                    _jsonOptions,
                    cancellationToken)
                .ConfigureAwait(false) ?? throw new BinanceUnknownResponseException();
        }

        /// <summary>
        /// Gets the account information.
        /// </summary>
        public async Task<AccountResponseModel> GetAccountInfoAsync(AccountRequestModel model, CancellationToken cancellationToken = default)
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

        #region Helpers

        /// <summary>
        /// Caches type data at zero lookup cost.
        /// </summary>
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
}