using System.Runtime.Serialization;

namespace Outcompute.Trader.Trading.Exceptions;

[Serializable]
public class TraderException : Exception
{
    public TraderException()
    {
    }

    public TraderException(string message) : base(message)
    {
    }

    public TraderException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected TraderException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}