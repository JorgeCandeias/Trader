using System.Collections.Generic;

namespace Outcompute.Trader.Trading.Algorithms
{
    public interface IAlgoDependencyInfo
    {
        IEnumerable<string> GetTickers();
    }
}