using Microsoft.Extensions.DependencyInjection;

namespace Outcompute.Trader.Trading.Algorithms
{
    public interface IAlgoBuilder
    {
        public string Name { get; }

        public IServiceCollection Services { get; }
    }

    public interface IAlgoBuilder<TOptions> : IAlgoBuilder
    {
    }
}