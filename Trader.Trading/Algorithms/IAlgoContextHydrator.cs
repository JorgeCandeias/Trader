using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    internal interface IAlgoContextHydrator
    {
        Task HydrateSymbolAsync(AlgoContext context, string name, string symbol, CancellationToken cancellationToken = default);

        Task HydrateAllAsync(AlgoContext context, string name, string symbol, DateTime startTime, CancellationToken cancellationToken = default);
    }
}