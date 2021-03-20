using System.Threading;
using System.Threading.Tasks;

namespace Trader.Data
{
    public interface IOrdersRepository
    {
        Task AddOrderAsync(OrderQueryResult order, CancellationToken cancellationToken = default);
    }
}