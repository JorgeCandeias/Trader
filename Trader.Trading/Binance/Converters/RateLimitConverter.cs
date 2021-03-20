using AutoMapper;
using System;
using Trader.Data;

namespace Trader.Trading.Binance.Converters
{
    internal class RateLimitConverter : ITypeConverter<RateLimiterModel, RateLimit>
    {
        public RateLimit Convert(RateLimiterModel source, RateLimit destination, ResolutionContext context)
        {
            // quick path for null source
            if (source is null) return null!;

            // map the rate limit type
            var type = source.RateLimitType switch
            {
                null => RateLimitType.None,

                "REQUEST_WEIGHT" => RateLimitType.RequestWeight,
                "ORDERS" => RateLimitType.Orders,
                "RAW_REQUESTS" => RateLimitType.RawRequests,

                _ => throw new AutoMapperMappingException($"{nameof(source.RateLimitType)} '{source.RateLimitType}' is unknown")
            };

            // map the timespan
            var timespan = source.Interval switch
            {
                null => TimeSpan.Zero,

                "MINUTE" => TimeSpan.FromMinutes(source.IntervalNum),
                "SECOND" => TimeSpan.FromSeconds(source.IntervalNum),
                "DAY" => TimeSpan.FromDays(source.IntervalNum),

                _ => throw new AutoMapperMappingException($"{nameof(source.Interval)} '{source.Interval}' is unknown")
            };

            // done
            return new RateLimit(type, timespan, source.Limit);
        }
    }
}