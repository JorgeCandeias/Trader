using Outcompute.Trader.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Commands
{
    public interface IEnsureSingleOrderService
    {
        Task<bool> EnsureSingleOrderAsync(Symbol symbol, OrderSide side, OrderType type, TimeInForce timeInForce, decimal quantity, decimal price, bool redeemSavings, CancellationToken cancellationToken = default);
    }
}