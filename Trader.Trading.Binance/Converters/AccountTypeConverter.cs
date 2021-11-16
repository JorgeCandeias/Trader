using AutoMapper;
using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Binance.Converters;

internal class AccountTypeConverter : ITypeConverter<string, AccountType>, ITypeConverter<AccountType, string>
{
    public string Convert(AccountType source, string destination, ResolutionContext context)
    {
        return source switch
        {
            AccountType.None => null!,

            AccountType.Spot => "SPOT",

            _ => throw new ArgumentOutOfRangeException(nameof(source), $"Unknown {nameof(AccountType)} '{source}'")
        };
    }

    public AccountType Convert(string source, AccountType destination, ResolutionContext context)
    {
        return source switch
        {
            null => AccountType.None,

            "SPOT" => AccountType.Spot,

            _ => throw new ArgumentOutOfRangeException(nameof(source), $"Unknown {nameof(AccountType)} '{source}'")
        };
    }
}