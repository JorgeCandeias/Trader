namespace Outcompute.Trader.Trading.Watchdog;

internal class WatchdogEntry : IWatchdogEntry
{
    private readonly Func<IServiceProvider, CancellationToken, Task> _action;

    public WatchdogEntry(Func<IServiceProvider, CancellationToken, Task> action)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
    }

    public Task ExecuteAsync(IServiceProvider provider, CancellationToken cancellationToken = default)
    {
        return _action(provider, cancellationToken);
    }
}