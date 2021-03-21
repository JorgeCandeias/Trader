using System.Threading;
using System.Threading.Tasks;

namespace Trader.Data
{
    public interface ITraderRepository
    {
        Task<SortedOrderSet> GetOrdersAsync(string symbol, CancellationToken cancellationToken = default);

        Task SetOrderAsync(OrderQueryResult order, CancellationToken cancellationToken = default);

        Task<long> GetMaxOrderIdAsync(string symbol, CancellationToken cancellationToken = default);

        Task<long> GetMinTransientOrderIdAsync(string symbol, CancellationToken cancellationToken = default);

        Task<SortedOrderSet> GetTransientOrdersAsync(string symbol, OrderSide? orderSide = default, bool? significant = default, CancellationToken cancellationToken = default);
    }
}