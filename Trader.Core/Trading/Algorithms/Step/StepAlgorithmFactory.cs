using Microsoft.Extensions.DependencyInjection;
using System;

namespace Trader.Core.Trading.Algorithms.Step
{
    internal class StepAlgorithmFactory : IStepAlgorithmFactory
    {
        private readonly IServiceProvider _provider;

        public StepAlgorithmFactory(IServiceProvider provider)
        {
            _provider = provider;
        }

        public IStepAlgorithm Create(string name)
        {
            return ActivatorUtilities.CreateInstance<StepAlgorithm>(_provider, name);
        }
    }
}