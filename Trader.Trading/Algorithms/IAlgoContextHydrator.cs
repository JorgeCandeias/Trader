using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    internal interface IAlgoContextHydrator
    {
        Task HydrateAsync(AlgoContext context, string symbol, CancellationToken cancellationToken = default);

        Task HydrateAsync(AlgoContext context, string symbol, DateTime tick, CancellationToken cancellationToken = default);
    }
}