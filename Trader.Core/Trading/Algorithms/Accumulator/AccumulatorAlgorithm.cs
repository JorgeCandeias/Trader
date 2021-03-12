using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace Trader.Core.Trading.Algorithms.Accumulator
{
    internal class AccumulatorAlgorithm : IAccumulatorAlgorithm
    {
        private readonly string _name;
        private readonly AccumulatorAlgorithmOptions _options;

        public AccumulatorAlgorithm(string name, IOptionsSnapshot<AccumulatorAlgorithmOptions> options)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _options = options.Get(name);
        }

        public Task GoAsync()
        {
            return Task.CompletedTask;
        }
    }
}