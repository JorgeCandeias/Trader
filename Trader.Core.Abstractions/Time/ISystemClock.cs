namespace Outcompute.Trader.Core.Time;

public interface ISystemClock
{
    DateTime UtcNow { get; }
}