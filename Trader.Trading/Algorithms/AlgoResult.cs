using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public abstract class AlgoResult : IAlgoResult
    {
        public abstract Task ExecuteAsync();

        public static AlgoResult None { get; } = NullAlgoResult.Instance;
    }
}