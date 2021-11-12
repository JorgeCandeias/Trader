using Microsoft.Extensions.DependencyInjection;

namespace Outcompute.Trader.Trading.Algorithms
{
    public interface IAlgoTypeBuilder
    {
        public string TypeName { get; }

        public IServiceCollection Services { get; }
    }

    public interface IAlgoTypeBuilder<TAlgoOptions> : IAlgoTypeBuilder
    {
    }
}