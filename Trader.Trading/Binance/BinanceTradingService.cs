using AutoMapper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using Trader.Models;
using Trader.Models.Collections;

namespace Trader.Trading.Binance
{
    internal class BinanceTradingService : ITradingService, IHostedService
    {
        private readonly ILogger _logger;
        private readonly BinanceOptions _options;
        private readonly BinanceApiClient _client;
        private readonly BinanceUsageContext _usage;
        private readonly IMapper _mapper;

        public BinanceTradingService(ILogger<BinanceTradingService> logger, IOptions<BinanceOptions> options, BinanceApiClient client, BinanceUsageContext usage, IMapper mapper)
        {
            _logger = logger;
            _options = options.Value;
            _client = client;
            _usage = usage;
            _mapper = mapper;
        }

        private static string Name => nameof(BinanceTradingService);

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

        public async Task<ImmutableSortedTradeSet> GetAccountTradesAsync(GetAccountTrades model, CancellationToken cancellationToken = default)
        {
            var input = _mapper.Map<AccountTradesRequestModel>(model);

            var output = await _client
                .GetAccountTradesAsync(input, cancellationToken)
                .ConfigureAwait(false);

            return _mapper.Map<ImmutableSortedTradeSet>(output);
        }

        public async Task<ImmutableSortedOrderSet> GetOpenOrdersAsync(GetOpenOrders model, CancellationToken cancellationToken = default)
        {
            _ = model ?? throw new ArgumentNullException(nameof(model));

            var input = _mapper.Map<GetOpenOrdersRequestModel>(model);

            var output = await _client
                .GetOpenOrdersAsync(input, cancellationToken)
                .ConfigureAwait(false);

            return _mapper.Map<ImmutableSortedOrderSet>(output);
        }

        public async Task<OrderQueryResult> GetOrderAsync(OrderQuery model, CancellationToken cancellationToken = default)
        {
            _ = model ?? throw new ArgumentNullException(nameof(model));

            var input = _mapper.Map<GetOrderRequestModel>(model);

            var output = await _client
                .GetOrderAsync(input, cancellationToken)
                .ConfigureAwait(false);

            return _mapper.Map<OrderQueryResult>(output);
        }

        public async Task<ImmutableSortedOrderSet> GetAllOrdersAsync(GetAllOrders model, CancellationToken cancellationToken = default)
        {
            _ = model ?? throw new ArgumentNullException(nameof(model));

            var input = _mapper.Map<GetAllOrdersRequestModel>(model);

            var output = await _client
                .GetAllOrdersAsync(input, cancellationToken)
                .ConfigureAwait(false);

            return _mapper.Map<ImmutableSortedOrderSet>(output);
        }

        public async Task<OrderResult> CreateOrderAsync(Order model, CancellationToken cancellationToken = default)
        {
            _ = model ?? throw new ArgumentNullException(nameof(model));

            var input = _mapper.Map<NewOrderRequestModel>(model);

            var output = await _client
                .CreateOrderAsync(input, cancellationToken)
                .ConfigureAwait(false);

            return _mapper.Map<OrderResult>(output);
        }

        public async Task<CancelStandardOrderResult> CancelOrderAsync(CancelStandardOrder model, CancellationToken cancellationToken = default)
        {
            _ = model ?? throw new ArgumentNullException(nameof(model));

            var input = _mapper.Map<CancelOrderRequestModel>(model);

            var output = await _client
                .CancelOrderAsync(input, cancellationToken)
                .ConfigureAwait(false);

            return _mapper.Map<CancelStandardOrderResult>(output);
        }

        public async Task<AccountInfo> GetAccountInfoAsync(GetAccountInfo model, CancellationToken cancellationToken = default)
        {
            _ = model ?? throw new ArgumentNullException(nameof(model));

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

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return SyncLimitsAsync(cancellationToken);
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

        #endregion Helpers
    }
}