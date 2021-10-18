using Orleans;
using Outcompute.Trader.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.InMemory
{
    internal interface IInMemoryTradingServiceGrain : IGrainWithGuidKey
    {
        #region Orders

        Task<CancelStandardOrderResult> CancelOrderAsync(string symbol, long orderId);

        Task<OrderResult> CreateOrderAsync(string symbol, OrderSide side, OrderType type, TimeInForce? timeInForce, decimal? quantity, decimal? quoteOrderQuantity, decimal? price, string? newClientOrderId, decimal? stopPrice, decimal? icebergQuantity);

        Task<IReadOnlyCollection<OrderQueryResult>> GetAllOrdersAsync(string symbol, long? orderId, int? limit);

        Task<IReadOnlyCollection<OrderQueryResult>> GetOpenOrdersAsync(string symbol);

        Task<OrderQueryResult> GetOrderAsync(string symbol, long? orderId, string? originalClientOrderId);

        #endregion Orders

        Task<IReadOnlyCollection<FlexibleProductPosition>> GetFlexibleProductPositionsAsync(string asset);

        Task SetFlexibleProductPositionsAsync(IEnumerable<FlexibleProductPosition> items);

        Task<LeftDailyRedemptionQuotaOnFlexibleProduct?> TryGetLeftDailyRedemptionQuotaOnFlexibleProductAsync(
            string productId,
            FlexibleProductRedemptionType type);

        Task SetLeftDailyRedemptionQuotaOnFlexibleProductAsync(string productId, FlexibleProductRedemptionType type, LeftDailyRedemptionQuotaOnFlexibleProduct item);
    }
}