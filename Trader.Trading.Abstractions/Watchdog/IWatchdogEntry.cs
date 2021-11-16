namespace Outcompute.Trader.Trading.Watchdog;

public interface IWatchdogEntry
{
    Task ExecuteAsync(IServiceProvider provider, CancellationToken cancellationToken = default);
}