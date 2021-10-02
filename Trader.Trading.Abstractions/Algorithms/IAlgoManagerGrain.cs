using Orleans;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public interface IAlgoManagerGrain : IGrainWithGuidKey
    {
        Task<IEnumerable<AlgoInfo>> GetAlgosAsync();
    }
}