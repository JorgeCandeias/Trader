using AutoMapper;
using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Binance.Converters;

internal class SavingsStatusConverter : ITypeConverter<SavingsStatus, string>, ITypeConverter<string, SavingsStatus>
{
    public string Convert(SavingsStatus source, string destination, ResolutionContext context)
    {
        return source switch
        {
            SavingsStatus.None => "",

            SavingsStatus.All => "ALL",
            SavingsStatus.Subscribable => "SUBSCRIBABLE",
            SavingsStatus.Unsubscribable => "UNSUBSCRIBABLE",

            _ => throw new ArgumentOutOfRangeException(nameof(source))
        };
    }

    public SavingsStatus Convert(string source, SavingsStatus destination, ResolutionContext context)
    {
        return source switch
        {
            null => SavingsStatus.None,
            "" => SavingsStatus.None,

            "ALL" => SavingsStatus.All,
            "SUBSCRIBABLE" => SavingsStatus.Subscribable,
            "UNSUBSCRIBABLE" => SavingsStatus.Unsubscribable,

            _ => throw new ArgumentOutOfRangeException(nameof(source))
        };
    }
}