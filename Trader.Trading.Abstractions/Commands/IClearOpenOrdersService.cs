using Outcompute.Trader.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Commands
{
    public interface IClearOpenOrdersService
    {
        Task ClearOpenOrdersAsync(Symbol symbol, OrderSide side, CancellationToken cancellationToken = default);
    }
}