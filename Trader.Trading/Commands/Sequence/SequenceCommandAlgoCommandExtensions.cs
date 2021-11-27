using Outcompute.Trader.Trading.Commands.Sequence;

namespace Outcompute.Trader.Trading.Commands
{
    public static class SequenceCommandAlgoCommandExtensions
    {
        /// <summary>
        /// Returns a command that executes the current command and then the specified command in sequence.
        /// </summary>
        public static IAlgoCommand Then(this IAlgoCommand before, IAlgoCommand after)
        {
            return new SequenceCommand(before, after);
        }
    }
}