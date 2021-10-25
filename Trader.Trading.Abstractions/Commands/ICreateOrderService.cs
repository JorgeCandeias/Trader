using Outcompute.Trader.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Commands
{
    public interface ICreateOrderService
    {
        Task<OrderResult> CreateOrderAsync(Symbol symbol, OrderType type, OrderSide side, TimeInForce timeInForce, decimal quantity, decimal price, string? tag, CancellationToken cancellationToken = default);
    }
}