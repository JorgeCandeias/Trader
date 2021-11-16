using Outcompute.Trader.Core.Time;

namespace Outcompute.Trader.Trading.Algorithms.Context.Configurators;

internal class AlgoContextTickTimeConfigurator : IAlgoContextConfigurator<AlgoContext>
{
    private readonly ISystemClock _clock;

    public AlgoContextTickTimeConfigurator(ISystemClock clock)
    {
        _clock = clock;
    }

    public ValueTask ConfigureAsync(AlgoContext context, string name, CancellationToken cancellationToken = default)
    {
        context.TickTime = _clock.UtcNow;

        return ValueTask.CompletedTask;
    }
}