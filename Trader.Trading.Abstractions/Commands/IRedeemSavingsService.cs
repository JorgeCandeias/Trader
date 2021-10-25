using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Commands
{
    public interface IRedeemSavingsService
    {
        Task<RedeemSavingsEvent> TryRedeemSavingsAsync(string asset, decimal amount, CancellationToken cancellationToken = default);
    }
}