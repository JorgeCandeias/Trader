using AutoMapper;
using System;

namespace Trader.Core.Trading.Binance.Converters
{
    internal class TimeZoneInfoConverter : ITypeConverter<string, TimeZoneInfo>
    {
        public TimeZoneInfo Convert(string source, TimeZoneInfo destination, ResolutionContext context)
        {
            return source switch
            {
                "UTC" => TimeZoneInfo.Utc,

                _ => throw new AutoMapperMappingException($"Unknown {nameof(TimeZoneInfo)} '{source}'")
            };
        }
    }
}