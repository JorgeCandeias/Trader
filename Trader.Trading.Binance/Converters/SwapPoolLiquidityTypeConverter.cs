using AutoMapper;
using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Binance.Converters;

internal class SwapPoolLiquidityTypeConverter : ITypeConverter<string, SwapPoolLiquidityType>, ITypeConverter<SwapPoolLiquidityType, string>
{
    public string Convert(SwapPoolLiquidityType source, string destination, ResolutionContext context)
    {
        return source switch
        {
            SwapPoolLiquidityType.Single => "SINGLE",
            SwapPoolLiquidityType.Combination => "COMBINATION",

            _ => throw new ArgumentOutOfRangeException(nameof(source))
        };
    }

    public SwapPoolLiquidityType Convert(string source, SwapPoolLiquidityType destination, ResolutionContext context)
    {
        return source switch
        {
            "SINGLE" => SwapPoolLiquidityType.Single,
            "COMBINATION" => SwapPoolLiquidityType.Combination,

            _ => throw new ArgumentOutOfRangeException(nameof(source))
        };
    }
}