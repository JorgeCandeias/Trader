using Microsoft.Extensions.DependencyInjection;

namespace Outcompute.Trader.Trading.Algorithms
{
    internal class AlgoTypeBuilder : IAlgoTypeBuilder
    {
        public AlgoTypeBuilder(string typeName, IServiceCollection services)
        {
            TypeName = typeName;
            Services = services;
        }

        public string TypeName { get; }

        public IServiceCollection Services { get; }
    }
}