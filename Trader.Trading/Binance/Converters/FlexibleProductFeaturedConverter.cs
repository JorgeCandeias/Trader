using AutoMapper;
using System;
using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Binance.Converters
{
    internal class FlexibleProductFeaturedConverter : ITypeConverter<FlexibleProductFeatured, string>, ITypeConverter<string, FlexibleProductFeatured>
    {
        public string Convert(FlexibleProductFeatured source, string destination, ResolutionContext context)
        {
            return source switch
            {
                FlexibleProductFeatured.None => "",

                FlexibleProductFeatured.All => "ALL",
                FlexibleProductFeatured.True => "TRUE",

                _ => throw new ArgumentOutOfRangeException(nameof(source))
            };
        }

        public FlexibleProductFeatured Convert(string source, FlexibleProductFeatured destination, ResolutionContext context)
        {
            return source switch
            {
                null => FlexibleProductFeatured.None,
                "" => FlexibleProductFeatured.None,

                "ALL" => FlexibleProductFeatured.All,
                "TRUE" => FlexibleProductFeatured.True,

                _ => throw new ArgumentOutOfRangeException(nameof(source))
            };
        }
    }
}