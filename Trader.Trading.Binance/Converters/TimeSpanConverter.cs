using AutoMapper;
using System;

namespace Outcompute.Trader.Trading.Binance.Converters
{
    internal class TimeSpanConverter : ITypeConverter<TimeSpan, long>, ITypeConverter<long, TimeSpan>
    {
        public TimeSpan Convert(long source, TimeSpan destination, ResolutionContext context)
        {
            return TimeSpan.FromMilliseconds(source);
        }

        public long Convert(TimeSpan source, long destination, ResolutionContext context)
        {
            return (long)source.TotalMilliseconds;
        }
    }
}