using AutoMapper;
using System;
using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Binance.Converters
{
    internal class FlexibleProductStatusConverter : ITypeConverter<FlexibleProductStatus, string>, ITypeConverter<string, FlexibleProductStatus>
    {
        public string Convert(FlexibleProductStatus source, string destination, ResolutionContext context)
        {
            return source switch
            {
                FlexibleProductStatus.None => "",

                FlexibleProductStatus.All => "ALL",
                FlexibleProductStatus.Subscribable => "SUBSCRIBABLE",
                FlexibleProductStatus.Unsubscribable => "UNSUBSCRIBABLE",

                _ => throw new ArgumentOutOfRangeException(nameof(source))
            };
        }

        public FlexibleProductStatus Convert(string source, FlexibleProductStatus destination, ResolutionContext context)
        {
            return source switch
            {
                null => FlexibleProductStatus.None,
                "" => FlexibleProductStatus.None,

                "ALL" => FlexibleProductStatus.All,
                "SUBSCRIBABLE" => FlexibleProductStatus.Subscribable,
                "UNSUBSCRIBABLE" => FlexibleProductStatus.Unsubscribable,

                _ => throw new ArgumentOutOfRangeException(nameof(source))
            };
        }
    }
}