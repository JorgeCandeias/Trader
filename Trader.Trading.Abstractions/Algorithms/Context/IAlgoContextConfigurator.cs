namespace Outcompute.Trader.Trading.Algorithms.Context;

public interface IAlgoContextConfigurator<in TAlgoContext>
    where TAlgoContext : IAlgoContext
{
    ValueTask ConfigureAsync(TAlgoContext context, string name, CancellationToken cancellationToken = default);
}