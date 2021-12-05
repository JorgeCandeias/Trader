using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;
using Outcompute.Trader.Trading.Exceptions;
using System.Diagnostics;

namespace Outcompute.Trader.Trading.Binance;

[ExcludeFromCodeCoverage(Justification = "Requires Integration Testing")]
internal partial class BinanceTradingService : ITradingService, IHostedService
{
    private readonly ILogger _logger;
    private readonly BinanceApiClient _client;
    private readonly BinanceUsageContext _usage;
    private readonly IMapper _mapper;
    private readonly ISystemClock _clock;
    private readonly IServiceProvider _provider;

    public BinanceTradingService(ILogger<BinanceTradingService> logger, BinanceApiClient client, BinanceUsageContext usage, IMapper mapper, ISystemClock clock, IServiceProvider provider)
    {
        _logger = logger;
        _client = client;
        _usage = usage;
        _mapper = mapper;
        _clock = clock;
        _provider = provider;
    }

    private const string TypeName = nameof(BinanceTradingService);

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

    public async Task<IReadOnlyCollection<SymbolPriceTicker>> GetSymbolPriceTickersAsync(CancellationToken cancellationToken = default)
    {
        var output = await _client
            .GetSymbolPriceTickersAsync(cancellationToken)
            .ConfigureAwait(false);

        return _mapper.Map<IReadOnlyCollection<SymbolPriceTicker>>(output);
    }

    public async Task<ImmutableSortedTradeSet> GetAccountTradesAsync(string symbol, long? fromId, int? limit, CancellationToken cancellationToken = default)
    {
        var model = new GetAccountTrades(symbol, null, null, fromId, limit, null, _clock.UtcNow);
        var input = _mapper.Map<GetAccountTradesRequest>(model);

        var output = await _client
            .GetAccountTradesAsync(input, cancellationToken)
            .ConfigureAwait(false);

        return _mapper.Map<ImmutableSortedTradeSet>(output);
    }

    public async Task<IReadOnlyCollection<OrderQueryResult>> GetOpenOrdersAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var model = new GetOpenOrders(symbol, null, _clock.UtcNow);
        var input = _mapper.Map<GetOpenOrdersRequest>(model);

        var output = await _client
            .GetOpenOrdersAsync(input, cancellationToken)
            .ConfigureAwait(false);

        return _mapper.Map<ImmutableSortedOrderSet>(output);
    }

    public async Task<OrderQueryResult> GetOrderAsync(string symbol, long? orderId, string? originalClientOrderId, CancellationToken cancellationToken = default)
    {
        var model = new OrderQuery(symbol, orderId, originalClientOrderId, null, _clock.UtcNow);
        var input = _mapper.Map<GetOrderRequest>(model);

        var output = await _client
            .GetOrderAsync(input, cancellationToken)
            .ConfigureAwait(false);

        return _mapper.Map<OrderQueryResult>(output);
    }

    public async Task<IReadOnlyCollection<OrderQueryResult>> GetAllOrdersAsync(string symbol, long? orderId, int? limit, CancellationToken cancellationToken = default)
    {
        var model = new GetAllOrders(symbol, orderId, null, null, limit, null, _clock.UtcNow);
        var input = _mapper.Map<GetAllOrdersRequest>(model);

        var output = await _client
            .GetAllOrdersAsync(input, cancellationToken)
            .ConfigureAwait(false);

        return _mapper.Map<ImmutableSortedOrderSet>(output);
    }

    public async Task<OrderResult> CreateOrderAsync(string symbol, OrderSide side, OrderType type, TimeInForce? timeInForce, decimal? quantity, decimal? quoteOrderQuantity, decimal? price, string? newClientOrderId, decimal? stopPrice, decimal? icebergQuantity, CancellationToken cancellationToken = default)
    {
        var model = new Order(symbol, side, type, timeInForce, quantity, quoteOrderQuantity, price, newClientOrderId, stopPrice, icebergQuantity, NewOrderResponseType.Full, null, _clock.UtcNow);
        var input = _mapper.Map<CreateOrderRequest>(model);

        var output = await _client
            .CreateOrderAsync(input, cancellationToken)
            .ConfigureAwait(false);

        return _mapper.Map<OrderResult>(output);
    }

    public async Task<CancelStandardOrderResult> CancelOrderAsync(string symbol, long orderId, CancellationToken cancellationToken = default)
    {
        var model = new CancelStandardOrder(symbol, orderId, null, null, null, _clock.UtcNow);
        var input = _mapper.Map<CancelOrderRequest>(model);

        CancelOrderResponse output;
        try
        {
            output = await _client
                .CancelOrderAsync(input, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (BinanceCodeException ex)
        {
            switch (ex.BinanceCode)
            {
                case -2011:
                    throw new UnknownOrderException(symbol, orderId, ex);

                default:
                    throw;
            }
        }

        return _mapper.Map<CancelStandardOrderResult>(output);
    }

    public async Task<AccountInfo> GetAccountInfoAsync(CancellationToken cancellationToken = default)
    {
        var model = new GetAccountInfo(null, _clock.UtcNow);
        var input = _mapper.Map<GetAccountInfoRequest>(model);

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

    public async Task<IReadOnlyCollection<Ticker>> Get24hTickerPriceChangeStatisticsAsync(CancellationToken cancellationToken = default)
    {
        BinanceApiContext.SkipSigning = true;

        var output = await _client
            .Get24hTickerPriceChangeStatisticsAsync(cancellationToken)
            .ConfigureAwait(false);

        return _mapper.Map<IReadOnlyCollection<Ticker>>(output);
    }

    public async Task<IReadOnlyCollection<Kline>> GetKlinesAsync(string symbol, KlineInterval interval, DateTime startTime, DateTime endTime, int limit, CancellationToken cancellationToken = default)
    {
        var model = new GetKlines(symbol, interval, startTime, endTime, limit);
        var input = _mapper.Map<GetKlinesRequest>(model);

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

    public async Task<IReadOnlyCollection<SavingsBalance>> GetSavingsBalancesAsync(
        string asset,
        CancellationToken cancellationToken = default)
    {
        var model = new GetFlexibleProductPosition(asset, null, _clock.UtcNow);

        var input = _mapper.Map<GetFlexibleProductPositionsRequest>(model);

        var output = await _client
            .GetFlexibleProductPositionsAsync(input, cancellationToken)
            .ConfigureAwait(false);

        return _mapper.Map<IReadOnlyCollection<SavingsBalance>>(output);
    }

    public async Task<SavingsQuota?> TryGetLeftDailyRedemptionQuotaOnFlexibleProductAsync(
        string productId,
        SavingsRedemptionType type,
        CancellationToken cancellationToken = default)
    {
        var model = new GetLeftDailyRedemptionQuotaOnFlexibleProduct(productId, type, null, _clock.UtcNow);

        var input = _mapper.Map<GetLeftDailyRedemptionQuotaOnFlexibleProductRequest>(model);

        var output = await _client
            .GetLeftDailyRedemptionQuotaOnFlexibleProductAsync(input, cancellationToken)
            .ConfigureAwait(false);

        return _mapper.Map<SavingsQuota>(output);
    }

    public async Task RedeemFlexibleProductAsync(
        string productId,
        decimal amount,
        SavingsRedemptionType type,
        CancellationToken cancellationToken = default)
    {
        var model = new RedeemFlexibleProduct(productId, amount, type, null, _clock.UtcNow);

        var input = _mapper.Map<RedeemFlexibleProductRequest>(model);

        await _client
            .RedeemFlexibleProductAsync(input, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyCollection<SavingsProduct>> GetSavingsProductsAsync(
        SavingsStatus status,
        SavingsFeatured featured,
        CancellationToken cancellationToken = default)
    {
        var page = 1;
        var list = new List<SavingsProduct>();

        while (true)
        {
            // get the page
            var result = await GetSavingsProductsAsync(status, featured, page, 100, cancellationToken)
                .ConfigureAwait(false);

            // keep the items
            list.AddRange(result);

            // stop if there are no more items to get
            if (result.Count < 100) break;

            // next page
            page++;
        }

        return list;
    }

    public async Task<IReadOnlyCollection<SavingsProduct>> GetSavingsProductsAsync(
        SavingsStatus status,
        SavingsFeatured featured,
        long? current,
        long? size,
        CancellationToken cancellationToken = default)
    {
        var model = new GetFlexibleProduct(status, featured, current, size, null, _clock.UtcNow);

        var input = _mapper.Map<GetFlexibleProductListRequest>(model);

        var output = await _client
            .GetFlexibleProductListAsync(input, cancellationToken)
            .ConfigureAwait(false);

        return _mapper.Map<IReadOnlyCollection<SavingsProduct>>(output);
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
            .PingUserDataStreamAsync(new PingUserDataStreamRequest(listenKey), cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task CloseUserDataStreamAsync(string listenKey, CancellationToken cancellationToken = default)
    {
        _ = listenKey ?? throw new ArgumentNullException(nameof(listenKey));

        BinanceApiContext.SkipSigning = true;

        await _client
            .CloseUserDataStreamAsync(new CloseUserDataStreamRequest(listenKey), cancellationToken)
            .ConfigureAwait(false);
    }

    #region Swap

    public async Task<IEnumerable<SwapPool>> GetSwapPoolsAsync(CancellationToken cancellationToken = default)
    {
        BinanceApiContext.SkipSigning = true;

        var result = await _client
            .GetSwapPoolsAsync(cancellationToken)
            .ConfigureAwait(false);

        return _mapper.Map<IEnumerable<SwapPool>>(result);
    }

    public async Task<SwapPoolLiquidity> GetSwapLiquidityAsync(long poolId, CancellationToken cancellationToken = default)
    {
        var model = new GetSwapPoolLiquidity(poolId, null, _clock.UtcNow);

        var input = _mapper.Map<GetSwapPoolLiquidityRequest>(model);

        var output = await _client
            .GetSwapPoolLiquidityAsync(input, cancellationToken)
            .ConfigureAwait(false);

        return _mapper.Map<SwapPoolLiquidity>(output);
    }

    public async Task<IEnumerable<SwapPoolLiquidity>> GetSwapLiquiditiesAsync(CancellationToken cancellationToken = default)
    {
        var model = new GetSwapPoolLiquidity(null, null, _clock.UtcNow);

        var input = _mapper.Map<GetSwapPoolLiquidityRequest>(model);

        var output = await _client
            .GetSwapPoolsLiquiditiesAsync(input, cancellationToken)
            .ConfigureAwait(false);

        return _mapper.Map<IEnumerable<SwapPoolLiquidity>>(output);
    }

    public async Task<SwapPoolOperation> AddSwapLiquidityAsync(long poolId, SwapPoolLiquidityType type, string asset, decimal quantity, CancellationToken cancellationToken = default)
    {
        var model = new AddSwapPoolLiquidity(poolId, type, asset, quantity, null, _clock.UtcNow);

        var input = _mapper.Map<AddSwapPoolLiquidityRequest>(model);

        var output = await _client
            .AddSwapPoolLiquidityAsync(input, cancellationToken)
            .ConfigureAwait(false);

        return _mapper.Map<SwapPoolOperation>(output);
    }

    public async Task<SwapPoolOperation> RemoveSwapLiquidityAsync(long poolId, SwapPoolLiquidityType type, decimal shareAmount, CancellationToken cancellationToken = default)
    {
        var model = new RemoveSwapPoolLiquidity(poolId, type, null, shareAmount, null, _clock.UtcNow);

        var input = _mapper.Map<RemoveSwapPoolLiquidityRequest>(model);

        var output = await _client
            .RemoveSwapPoolLiquidityAsync(input, cancellationToken)
            .ConfigureAwait(false);

        return _mapper.Map<SwapPoolOperation>(output);
    }

    public async Task<IEnumerable<SwapPoolConfiguration>> GetSwapPoolConfigurationsAsync(CancellationToken cancellationToken = default)
    {
        var model = new GetSwapPoolConfiguration(null, null, _clock.UtcNow);

        var input = _mapper.Map<GetSwapPoolConfigurationRequest>(model);

        var output = await _client
            .GetSwapPoolConfigurationsAsync(input, cancellationToken)
            .ConfigureAwait(false);

        return _mapper.Map<IEnumerable<SwapPoolConfiguration>>(output);
    }

    public async Task<SwapPoolLiquidityAddPreview> AddSwapPoolLiquidityPreviewAsync(long poolId, SwapPoolLiquidityType type, string quoteAsset, decimal quoteQuantity, CancellationToken cancellationToken = default)
    {
        var model = new AddSwapPoolLiquidityPreview(poolId, type, quoteAsset, quoteQuantity, null, _clock.UtcNow);

        var input = _mapper.Map<AddSwapPoolLiquidityPreviewRequest>(model);

        var output = await _client
            .AddSwapPoolLiquidityPreviewAsync(input, cancellationToken)
            .ConfigureAwait(false);

        return _mapper.Map<SwapPoolLiquidityAddPreview>(output);
    }

    #endregion Swap

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await SyncLimitsAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task<SwapPoolQuote> GetSwapPoolQuoteAsync(string quoteAsset, string baseAsset, decimal quoteQuantity, CancellationToken cancellationToken = default)
    {
        var model = new GetSwapPoolQuote(quoteAsset, baseAsset, quoteQuantity, null, _clock.UtcNow);

        var input = _mapper.Map<GetSwapPoolQuoteRequest>(model);

        var output = await _client.GetSwapPoolQuoteAsync(input, cancellationToken).ConfigureAwait(false);

        return _mapper.Map<SwapPoolQuote>(output);
    }

    #region Helpers

    private async Task SyncLimitsAsync(CancellationToken cancellationToken)
    {
        LogQueryingExchangeRateLimits(TypeName);

        var watch = Stopwatch.StartNew();

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

        LogQueriedExchangeRateLimitsInMs(TypeName, watch.ElapsedMilliseconds);
    }

    #endregion Helpers

    #region Logging

    [LoggerMessage(0, LogLevel.Information, "{Type} querying exchange rate limits...")]
    private partial void LogQueryingExchangeRateLimits(string type);

    [LoggerMessage(1, LogLevel.Information, "{Type} queried exchange rate limits in {ElapsedMs}ms")]
    private partial void LogQueriedExchangeRateLimitsInMs(string type, long elapsedMs);

    #endregion Logging
}