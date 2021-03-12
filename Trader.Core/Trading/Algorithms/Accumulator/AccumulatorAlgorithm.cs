using System;
using System.Threading.Tasks;

namespace Trader.Core.Trading.Algorithms.Accumulator
{
    internal class AccumulatorAlgorithm : IAccumulatorAlgorithm
    {
        private readonly string _name;

        public AccumulatorAlgorithm(string name)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public Task GoAsync()
        {
            return Task.CompletedTask;
        }
    }
}