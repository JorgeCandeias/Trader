using AutoMapper;

namespace Outcompute.Trader.Trading.Binance.Converters;

internal class DateTimeConverter : ITypeConverter<DateTime, long>, ITypeConverter<long, DateTime>
{
    public DateTime Convert(long source, DateTime destination, ResolutionContext context)
    {
        return DateTime.UnixEpoch.AddMilliseconds(source);
    }

    public long Convert(DateTime source, long destination, ResolutionContext context)
    {
        return (long)source.Subtract(DateTime.UnixEpoch).TotalMilliseconds;
    }
}