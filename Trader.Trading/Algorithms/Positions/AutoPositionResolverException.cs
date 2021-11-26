using System.Runtime.Serialization;

namespace Outcompute.Trader.Trading.Algorithms.Positions;

[Serializable]
public class AutoPositionResolverException : Exception
{
    public AutoPositionResolverException()
    {
    }

    public AutoPositionResolverException(string? message) : base(message)
    {
    }

    public AutoPositionResolverException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    protected AutoPositionResolverException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}