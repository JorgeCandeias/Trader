using Orleans;

namespace Outcompute.Trader.Trading.Algorithms;

public interface IAlgoManagerGrain : IGrainWithGuidKey
{
    Task<IReadOnlyCollection<AlgoInfo>> GetAlgosAsync();
}