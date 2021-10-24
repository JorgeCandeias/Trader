using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    /// <summary>
    /// Handles algo results.
    /// </summary>
    public interface IAlgoResultHandler
    {
        Task HandleAsync(IAlgoResult result);
    }
}