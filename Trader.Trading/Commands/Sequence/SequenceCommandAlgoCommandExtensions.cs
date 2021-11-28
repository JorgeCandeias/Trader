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
            // if the source command is already a sequence then reuse its commands
            // this makes exception call stacks look tidier when one of them throws
            if (before is SequenceCommand sequence)
            {
                return new SequenceCommand(sequence.Commands.Append(after));
            }

            // otherwise wrap them both in a sequence command
            return new SequenceCommand(before, after);
        }
    }
}