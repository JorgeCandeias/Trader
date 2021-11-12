using Microsoft.Extensions.DependencyInjection;

namespace Outcompute.Trader.Trading.Algorithms
{
    internal class AlgoBuilder : IAlgoBuilder
    {
        public AlgoBuilder(string name, IServiceCollection services)
        {
            Name = name;
            Services = services;
        }

        public string Name { get; }

        public IServiceCollection Services { get; }
    }

    internal class AlgoBuilder<TOptions> : AlgoBuilder, IAlgoBuilder<TOptions>
    {
        public AlgoBuilder(string name, IServiceCollection services) :
            base(name, services)
        {
        }
    }
}