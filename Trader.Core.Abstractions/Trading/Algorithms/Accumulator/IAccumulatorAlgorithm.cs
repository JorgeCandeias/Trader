using System.Threading.Tasks;

namespace Trader.Core.Trading.Algorithms.Accumulator
{
    public interface IAccumulatorAlgorithm : ITradingAlgorithm
    {
        Task GoAsync();
    }
}