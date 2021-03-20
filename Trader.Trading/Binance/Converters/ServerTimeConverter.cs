using AutoMapper;
using System;

namespace Trader.Trading.Binance.Converters
{
    internal class ServerTimeConverter : ITypeConverter<ServerTimeModel, DateTime>
    {
        public DateTime Convert(ServerTimeModel source, DateTime destination, ResolutionContext context)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return DateTime.UnixEpoch.AddMilliseconds(source.ServerTime);
        }
    }
}