using AutoMapper;
using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Binance.Converters;

internal class SavingsRedemptionTypeConverter : ITypeConverter<SavingsRedemptionType, string>, ITypeConverter<string, SavingsRedemptionType>
{
    public SavingsRedemptionType Convert(string source, SavingsRedemptionType destination, ResolutionContext context)
    {
        return source switch
        {
            null => SavingsRedemptionType.None,

            "" => SavingsRedemptionType.None,
            "FAST" => SavingsRedemptionType.Fast,
            "NORMAL" => SavingsRedemptionType.Normal,

            _ => throw new ArgumentOutOfRangeException(nameof(source))
        };
    }

    public string Convert(SavingsRedemptionType source, string destination, ResolutionContext context)
    {
        return source switch
        {
            SavingsRedemptionType.None => "",

            SavingsRedemptionType.Fast => "FAST",
            SavingsRedemptionType.Normal => "NORMAL",

            _ => throw new ArgumentOutOfRangeException(nameof(source))
        };
    }
}