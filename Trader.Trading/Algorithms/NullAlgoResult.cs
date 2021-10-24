using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public class NullAlgoResult : AlgoResult
    {
        private NullAlgoResult()
        {
        }

        public override Task ExecuteAsync() => Task.CompletedTask;

        public static NullAlgoResult Instance { get; } = new NullAlgoResult();
    }
}