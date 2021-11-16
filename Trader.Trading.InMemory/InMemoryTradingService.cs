using Orleans;
using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;

namespace Outcompute.Trader.Trading.InMemory;

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
        return Task.CompletedTask;
    }

    public Task<string> CreateUserDataStreamAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Guid.NewGuid().ToString());
    }

    public Task Set24hTickerPriceChangeStatisticsAsync(Ticker ticker, CancellationToken cancellationToken = default)
    {
        return _grain.Set24hTickerPriceChangeStatisticsAsync(ticker);
    }

    public Task<Ticker> Get24hTickerPriceChangeStatisticsAsync(string symbol, CancellationToken cancellationToken = default)
    {
        return _grain.Get24hTickerPriceChangeStatisticsAsync(symbol);
    }

    public Task<IReadOnlyCollection<Ticker>> Get24hTickerPriceChangeStatisticsAsync(CancellationToken cancellationToken = default)
    {
        return _grain.Get24hTickerPriceChangeStatisticsAsync();
    }

    public Task<AccountInfo> GetAccountInfoAsync(CancellationToken cancellationToken = default)
    {
        return _grain.GetAccountInfoAsync();
    }

    public Task<ImmutableSortedTradeSet> GetAccountTradesAsync(string symbol, long? fromId, int? limit, CancellationToken cancellationToken = default)
    {
        return _grain.GetAccountTradesAsync(symbol, fromId, limit);
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

    public Task<IReadOnlyCollection<SymbolPriceTicker>> GetSymbolPriceTickersAsync(CancellationToken cancellationToken = default)
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

    public Task SetAccountInfoAsync(AccountInfo info, CancellationToken cancellationToken = default)
    {
        return _grain.SetAccountInfoAsync(info);
    }

    public Task SetAccountTradeAsync(AccountTrade trade, CancellationToken cancellationToken = default)
    {
        return _grain.SetAccountTradeAsync(trade);
    }

    public Task<IEnumerable<SwapPool>> GetSwapPoolsAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<SwapPoolLiquidity> GetSwapLiquidityAsync(long poolId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<SwapPoolLiquidity>> GetSwapLiquiditiesAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<SwapPoolOperation> AddSwapLiquidityAsync(long poolId, SwapPoolLiquidityType type, string asset, decimal quantity, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<SwapPoolOperation> RemoveSwapLiquidityAsync(long poolId, SwapPoolLiquidityType type, decimal shareAmount, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<SwapPoolConfiguration>> GetSwapPoolConfigurationsAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<SwapPoolLiquidityAddPreview> AddSwapPoolLiquidityPreviewAsync(long poolId, SwapPoolLiquidityType type, string quoteAsset, decimal quoteQuantity, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyCollection<SavingsProduct>> GetSavingsProductsAsync(SavingsStatus status, SavingsFeatured featured, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<SwapPoolQuote> GetSwapPoolQuoteAsync(string quoteAsset, string baseAsset, decimal quoteQuantity, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}