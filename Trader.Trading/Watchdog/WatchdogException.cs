using System.Runtime.Serialization;

namespace Outcompute.Trader.Trading.Watchdog;

[Serializable]
public class WatchdogException : Exception
{
    public WatchdogException()
    {
    }

    public WatchdogException(string message) : base(message)
    {
    }

    public WatchdogException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected WatchdogException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base()
    {
    }
}