using Outcompute.Trader.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers
{
    public interface ISymbolProvider
    {
        ValueTask<Symbol?> TryGetSymbolAsync(string symbol, CancellationToken cancellationToken = default);
    }
}