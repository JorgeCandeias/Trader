using Orleans;
using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;

namespace Outcompute.Trader.Trading.InMemory;

internal interface IInMemoryTradingServiceGrain : IGrainWithGuidKey
{
    #region Orders

    Task<CancelStandardOrderResult> CancelOrderAsync(string symbol, long orderId);

    Task<OrderResult> CreateOrderAsync(string symbol, OrderSide side, OrderType type, TimeInForce? timeInForce, decimal? quantity, decimal? quoteOrderQuantity, decimal? price, string? newClientOrderId, decimal? stopPrice, decimal? icebergQuantity);

    Task<IReadOnlyCollection<OrderQueryResult>> GetAllOrdersAsync(string symbol, long? orderId, int? limit);

    Task<IReadOnlyCollection<OrderQueryResult>> GetOpenOrdersAsync(string symbol);

    Task<OrderQueryResult> GetOrderAsync(string symbol, long? orderId, string? originalClientOrderId);

    #endregion Orders

    #region Exchange

    Task<ExchangeInfo> GetExchangeInfoAsync();

    Task SetExchangeInfoAsync(ExchangeInfo info);

    #endregion Exchange

    Task<IReadOnlyCollection<SavingsBalance>> GetFlexibleProductPositionsAsync(string asset);

    Task SetFlexibleProductPositionsAsync(IEnumerable<SavingsBalance> items);

    Task<SavingsQuota?> TryGetLeftDailyRedemptionQuotaOnFlexibleProductAsync(
        string productId,
        SavingsRedemptionType type);

    Task SetLeftDailyRedemptionQuotaOnFlexibleProductAsync(string productId, SavingsRedemptionType type, SavingsQuota item);

    Task Set24hTickerPriceChangeStatisticsAsync(Ticker ticker);

    Task<Ticker> Get24hTickerPriceChangeStatisticsAsync(string symbol);

    Task<IReadOnlyCollection<Ticker>> Get24hTickerPriceChangeStatisticsAsync();

    Task<AccountInfo> GetAccountInfoAsync();

    Task SetAccountInfoAsync(AccountInfo info);

    Task SetAccountTradeAsync(AccountTrade trade);

    Task<ImmutableSortedTradeSet> GetAccountTradesAsync(string symbol, long? fromId, int? limit);
}