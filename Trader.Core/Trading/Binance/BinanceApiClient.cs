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
using Trader.Core.Time;

namespace Trader.Core.Trading.Binance
{
    internal class BinanceApiClient
    {
        private readonly HttpClient _client;
        private readonly IMapper _mapper;
        private readonly ISystemClock _clock;
        private readonly ObjectPool<StringBuilder> _pool;

        public BinanceApiClient(HttpClient client, IMapper mapper, ISystemClock clock, ObjectPool<StringBuilder> pool)
        {
            _client = client;
            _mapper = mapper;
            _clock = clock;
            _pool = pool;
        }

        private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        #region General Endpoints

        public async Task<bool> PingAsync()
        {
            var response = await _client.GetAsync("/api/v3/ping");

            return response.IsSuccessStatusCode;
        }

        public async Task<DateTime> GetTimeAsync()
        {
            var result = await _client.GetFromJsonAsync<ServerTimeModel>("/api/v3/time");

            return _mapper.Map<DateTime>(result);
        }

        public async Task<ExchangeInfoModel> GetExchangeInfoAsync(CancellationToken cancellationToken = default)
        {
            return await _client.GetFromJsonAsync<ExchangeInfoModel>("/api/v3/exchangeInfo", cancellationToken) ?? throw new BinanceUnknownResponseException();
        }

        #endregion General Endpoints

        #region Market Data Endpoints

        public async Task<OrderBook> GetOrderBookAsync(string symbol)
        {
            var result = await _client.GetFromJsonAsync<OrderBookModel>($"/api/v3/depth?symbol={HttpUtility.UrlEncode(symbol)}");

            return _mapper.Map<OrderBook>(result);
        }

        public async Task<IEnumerable<Trade>> GetRecentTradesAsync(string symbol)
        {
            var result = await _client.GetFromJsonAsync<TradeModel[]>($"/api/v3/trades?symbol={HttpUtility.UrlEncode(symbol)}");

            return _mapper.Map<IEnumerable<Trade>>(result);
        }

        public async Task<IEnumerable<Trade>> GetHistoricalTradesAsync(string symbol)
        {
            var result = await _client.GetFromJsonAsync<TradeModel[]>($"/api/v3/historicalTrades?symbol={HttpUtility.UrlEncode(symbol)}");

            return _mapper.Map<IEnumerable<Trade>>(result);
        }

        public async Task<IEnumerable<AggTrade>> GetAggTradesAsync(string symbol)
        {
            var result = await _client.GetFromJsonAsync<IDictionary<string, JsonElement>[]>($"/api/v3/aggTrades?symbol={HttpUtility.UrlEncode(symbol)}", _jsonOptions);

            return _mapper.Map<IEnumerable<AggTrade>>(result);
        }

        public async Task<IEnumerable<Kline>> GetKlinesAsync(string symbol, KlineInterval interval)
        {
            var intervalModel = _mapper.Map<string>(interval);

            var result = await _client.GetFromJsonAsync<JsonElement[][]>(
                $"/api/v3/klines?symbol={HttpUtility.UrlEncode(symbol)}&interval={HttpUtility.UrlEncode(intervalModel)}");

            return _mapper.Map<IEnumerable<Kline>>(result);
        }

        public async Task<AvgPrice> GetAvgPriceAsync(string symbol)
        {
            var result = await _client.GetFromJsonAsync<AvgPriceModel>(
                $"/api/v3/avgPrice?symbol={HttpUtility.UrlEncode(symbol)}");

            return _mapper.Map<AvgPrice>(result);
        }

        public async Task<Ticker> Get24hTickerPriceChangeStatisticsAsync(string symbol)
        {
            var result = await _client.GetFromJsonAsync<TickerModel>(
                $"/api/v3/ticker/24hr?symbol={HttpUtility.UrlEncode(symbol)}");

            return _mapper.Map<Ticker>(result);
        }

        public async Task<SymbolPriceTickerModel> GetSymbolPriceTickerAsync(string symbol, CancellationToken cancellationToken = default)
        {
            _ = symbol ?? throw new ArgumentNullException(nameof(symbol));

            BinanceApiContext.SkipSigning = true;

            return await _client.GetFromJsonAsync<SymbolPriceTickerModel>($"/api/v3/ticker/price?symbol={HttpUtility.UrlEncode(symbol)}", cancellationToken) ?? throw new BinanceUnknownResponseException();
        }

        public async Task<ImmutableList<SymbolPriceTicker>> GetSymbolPriceTickersAsync()
        {
            var result = await _client.GetFromJsonAsync<IEnumerable<SymbolPriceTickerModel>>(
                $"/api/v3/ticker/price");

            return _mapper.Map<ImmutableList<SymbolPriceTicker>>(result);
        }

        public async Task<SymbolOrderBookTicker> GetSymbolOrderBookTickerAsync(string symbol)
        {
            var result = await _client.GetFromJsonAsync<SymbolOrderBookTickerModel>(
                $"/api/v3/ticker/bookTicker?symbol={HttpUtility.UrlEncode(symbol)}");

            return _mapper.Map<SymbolOrderBookTicker>(result);
        }

        public async Task<IEnumerable<SymbolOrderBookTicker>> GetSymbolOrderBookTickersAsync()
        {
            var result = await _client.GetFromJsonAsync<IEnumerable<SymbolOrderBookTickerModel>>(
                $"/api/v3/ticker/bookTicker");

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

            var response = await _client.PostAsync(Combine("/api/v3/order", model), EmptyHttpContent.Instance, cancellationToken);

            return await response.Content.ReadFromJsonAsync<NewOrderResponseModel>(_jsonOptions, cancellationToken) ?? throw new BinanceUnknownResponseException();
        }

        /// <summary>
        /// Gets the status of the specified order.
        /// </summary>
        public Task<OrderQueryResult> GetOrderAsync(OrderQuery query, CancellationToken cancellationToken = default)
        {
            _ = query ?? throw new ArgumentNullException(nameof(query));
            _ = query.Symbol ?? throw new ArgumentException($"{nameof(OrderQuery.Symbol)} is required");

            return GetAsync<GetOrderRequestModel, GetOrderResponseModel, OrderQueryResult>(
                "/api/v3/order",
                query,
                cancellationToken);
        }

        /// <summary>
        /// Cancels the specified order.
        /// </summary>
        public async Task<CancelOrderResponseModel> CancelOrderAsync(CancelOrderRequestModel model, CancellationToken cancellationToken = default)
        {
            _ = model ?? throw new ArgumentNullException(nameof(model));
            _ = model.Symbol ?? throw new ArgumentException($"{nameof(OrderQuery.Symbol)} is required");

            var output = await _client.DeleteAsync(Combine("/api/v3/order", model), cancellationToken);

            return await output.Content.ReadFromJsonAsync<CancelOrderResponseModel>(_jsonOptions, cancellationToken) ?? throw new BinanceUnknownResponseException();
        }

        /// <summary>
        /// Cancels all open orders.
        /// </summary>
        public Task<ImmutableList<CancelOrderResultBase>> CancelAllOrdersAsync(CancelAllOrders cancellation, CancellationToken cancellationToken = default)
        {
            _ = cancellation ?? throw new ArgumentNullException(nameof(cancellation));
            _ = cancellation.Symbol ?? throw new ArgumentException($"{nameof(OrderQuery.Symbol)} is required");

            return DeleteAsync<CancelAllOrdersRequestModel, IEnumerable<CancelAllOrdersResponseModel>, ImmutableList<CancelOrderResultBase>>(
                "/api/v3/openOrders",
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

            return await _client.GetFromJsonAsync<IEnumerable<GetOrderResponseModel>>(Combine("/api/v3/openOrders", model), _jsonOptions, cancellationToken) ?? throw new BinanceUnknownResponseException();
        }

        /// <summary>
        /// Gets all orders.
        /// </summary>
        public async Task<IEnumerable<GetOrderResponseModel>> GetAllOrdersAsync(GetAllOrdersRequestModel model, CancellationToken cancellationToken = default)
        {
            _ = model ?? throw new ArgumentNullException(nameof(model));
            _ = model.Symbol ?? throw new ArgumentException($"{nameof(GetOpenOrders.Symbol)} is required");

            return await _client.GetFromJsonAsync<IEnumerable<GetOrderResponseModel>>(Combine("/api/v3/allOrders", model), _jsonOptions, cancellationToken) ?? throw new BinanceUnknownResponseException();
        }

        /// <summary>
        /// Gets the account information.
        /// </summary>
        public async Task<AccountResponseModel> GetAccountInfoAsync(AccountRequestModel model, CancellationToken cancellationToken = default)
        {
            _ = model ?? throw new ArgumentNullException(nameof(model));

            return await _client.GetFromJsonAsync<AccountResponseModel>(Combine("/api/v3/account", model), _jsonOptions, cancellationToken) ?? throw new BinanceUnknownResponseException();
        }

        /// <summary>
        /// Gets the account trades.
        /// </summary>
        public async Task<IEnumerable<AccountTradesResponseModel>> GetAccountTradesAsync(AccountTradesRequestModel model, CancellationToken cancellationToken = default)
        {
            _ = model ?? throw new ArgumentNullException(nameof(model));
            _ = model.Symbol ?? throw new ArgumentException($"{nameof(GetOpenOrders.Symbol)} is required");

            return await _client.GetFromJsonAsync<IEnumerable<AccountTradesResponseModel>>(Combine("/api/v3/myTrades", model), cancellationToken) ?? throw new BinanceUnknownResponseException();
        }

        #endregion Account Endpoints

        #region Helpers

        /// <summary>
        /// Caches type data at zero lookup cost.
        /// </summary>
        private static class TypeCache<T>
        {
            public static TypeAccessor TypeAccessor { get; } = TypeAccessor.Create(typeof(T));

            public static ImmutableArray<(string Name, string LowerName)> Names { get; } = TypeAccessor
                .GetMembers()
                .Select(x => (x.Name, char.ToLowerInvariant(x.Name[0]) + x.Name[1..]))
                .ToImmutableArray();
        }

        private string Combine<T>(string requestUri, T data)
        {
            var builder = _pool.Get();

            try
            {
                builder.Append(requestUri).Append('?');

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

                return builder.ToString();
            }
            finally
            {
                _pool.Return(builder);
            }
        }

        private async Task<TResult> GetAsync<TRequest, TResponse, TResult>(string requestUri, object data, CancellationToken cancellationToken = default)
        {
            var request = _mapper.Map<TRequest>(data);

            var response = await _client.GetFromJsonAsync<TResponse>(Combine(requestUri, request), _jsonOptions, cancellationToken);

            return _mapper.Map<TResult>(response);
        }

        private async Task<TResult> PostAsync<TRequest, TResponse, TResult>(string requestUri, object data, CancellationToken cancellationToken = default)
        {
            var request = _mapper.Map<TRequest>(data);

            var response = await _client.PostAsync(Combine(requestUri, request), EmptyHttpContent.Instance, cancellationToken);

            var typed = await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions, cancellationToken);

            return _mapper.Map<TResult>(typed);
        }

        private async Task<TResult> DeleteAsync<TRequest, TResponse, TResult>(string requestUri, object data, CancellationToken cancellationToken = default)
        {
            var request = _mapper.Map<TRequest>(data);

            var response = await _client.DeleteAsync(Combine(requestUri, request), cancellationToken);

            var typed = await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions, cancellationToken);

            return _mapper.Map<TResult>(typed);
        }

        #endregion Helpers
    }
}