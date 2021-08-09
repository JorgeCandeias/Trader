using AutoMapper;
using System;
using Trader.Models;

namespace Trader.Trading.Binance.Converters
{
    internal class FlexibleProductRedemptionTypeConverter : ITypeConverter<FlexibleProductRedemptionType, string>, ITypeConverter<string, FlexibleProductRedemptionType>
    {
        public FlexibleProductRedemptionType Convert(string source, FlexibleProductRedemptionType destination, ResolutionContext context)
        {
            return source switch
            {
                null => FlexibleProductRedemptionType.None,

                "" => FlexibleProductRedemptionType.None,
                "FAST" => FlexibleProductRedemptionType.Fast,
                "NORMAL" => FlexibleProductRedemptionType.Normal,

                _ => throw new ArgumentOutOfRangeException(nameof(source))
            };
        }

        public string Convert(FlexibleProductRedemptionType source, string destination, ResolutionContext context)
        {
            return source switch
            {
                FlexibleProductRedemptionType.None => "",

                FlexibleProductRedemptionType.Fast => "FAST",
                FlexibleProductRedemptionType.Normal => "NORMAL",

                _ => throw new ArgumentOutOfRangeException(nameof(source))
            };
        }
    }
}