using AutoMapper;
using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Binance.Converters;

internal class ExecutionTypeConverter : ITypeConverter<string, ExecutionType>, ITypeConverter<ExecutionType, string>
{
    public string Convert(ExecutionType source, string destination, ResolutionContext context)
    {
        return source switch
        {
            ExecutionType.None => null!,

            ExecutionType.New => "NEW",
            ExecutionType.Cancelled => "CANCELED",
            ExecutionType.Replaced => "REPLACED",
            ExecutionType.Rejected => "REJECTED",
            ExecutionType.Trade => "TRADE",
            ExecutionType.Expired => "EXPIRED",

            _ => throw new ArgumentOutOfRangeException(nameof(source))
        };
    }

    public ExecutionType Convert(string source, ExecutionType destination, ResolutionContext context)
    {
        return source switch
        {
            null => ExecutionType.None,
            "" => ExecutionType.None,

            "NEW" => ExecutionType.New,
            "CANCELED" => ExecutionType.Cancelled,
            "REPLACED" => ExecutionType.Replaced,
            "REJECTED" => ExecutionType.Rejected,
            "TRADE" => ExecutionType.Trade,
            "EXPIRED" => ExecutionType.Expired,

            _ => throw new ArgumentOutOfRangeException(nameof(source))
        };
    }
}