using Microsoft.Extensions.DependencyInjection;

namespace Outcompute.Trader.Trading.Algorithms
{
    public interface IAlgoTypeBuilder
    {
        public IServiceCollection Services { get; }
    }
}