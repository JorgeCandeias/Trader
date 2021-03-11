using System.Threading.Tasks;

namespace Trader.Core.Trading.Algorithms.Step
{
    public interface IStepAlgorithm : ITradingAlgorithm
    {
        Task GoAsync(ExchangeInfo exchangeInfo);
    }
}