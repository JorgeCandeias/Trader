using AutoMapper;
using System;

namespace Trader.Core.Trading.Binance.Converters
{
    internal class TimeInForceConverter : ITypeConverter<TimeInForce, string>, ITypeConverter<string, TimeInForce>
    {
        public TimeInForce Convert(string source, TimeInForce destination, ResolutionContext context)
        {
            return source switch
            {
                null => TimeInForce.None,

                "GTC" => TimeInForce.GoodTillCanceled,
                "IOC" => TimeInForce.ImmediateOrCancel,
                "FOK" => TimeInForce.FillOrKill,

                _ => throw new AutoMapperMappingException($"Unknown {nameof(TimeInForce)} '{source}'")
            };
        }

        public string Convert(TimeInForce source, string destination, ResolutionContext context)
        {
            return source switch
            {
                TimeInForce.None => null!,

                TimeInForce.GoodTillCanceled => "GTC",
                TimeInForce.ImmediateOrCancel => "IOC",
                TimeInForce.FillOrKill => "FOK",

                _ => throw new ArgumentOutOfRangeException($"Unknown {nameof(TimeInForce)} '{source}'")
            };
        }
    }
}