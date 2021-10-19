using Outcompute.Trader.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers
{
    public interface IExchangeInfoProvider
    {
        Task<ExchangeInfo> GetExchangeInfoAsync(CancellationToken cancellationToken = default);

        Task<Symbol?> TryGetSymbolAsync(string name, CancellationToken cancellationToken = default);
    }
}