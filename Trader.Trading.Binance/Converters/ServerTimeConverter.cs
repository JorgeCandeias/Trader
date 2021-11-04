using AutoMapper;
using System;

namespace Outcompute.Trader.Trading.Binance.Converters
{
    internal class ServerTimeConverter : ITypeConverter<ApiServerTime, DateTime>
    {
        public DateTime Convert(ApiServerTime source, DateTime destination, ResolutionContext context)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return DateTime.UnixEpoch.AddMilliseconds(source.ServerTime);
        }
    }
}