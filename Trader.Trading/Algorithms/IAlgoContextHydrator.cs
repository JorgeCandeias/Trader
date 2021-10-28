using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    internal interface IAlgoContextHydrator
    {
        Task HydrateSymbolAsync(AlgoContext context, string symbol, CancellationToken cancellationToken = default);

        Task HydrateAllAsync(AlgoContext context, string symbol, CancellationToken cancellationToken = default);
    }
}