using AutoMapper;
using System;

namespace Trader.Core.Trading.Binance.Converters
{
    internal class KlineIntervalConverter : ITypeConverter<KlineInterval, string>
    {
        public string Convert(KlineInterval source, string destination, ResolutionContext context)
        {
            return source switch
            {
                KlineInterval.None => null!,

                KlineInterval.Minutes1 => "1m",
                KlineInterval.Minutes3 => "3m",
                KlineInterval.Minutes5 => "5m",
                KlineInterval.Minutes15 => "15m",
                KlineInterval.Minutes30 => "30m",
                KlineInterval.Hours1 => "1h",
                KlineInterval.Hours2 => "2h",
                KlineInterval.Hours4 => "4h",
                KlineInterval.Hours6 => "6h",
                KlineInterval.Hours8 => "8h",
                KlineInterval.Hours12 => "12h",
                KlineInterval.Days1 => "1d",
                KlineInterval.Days3 => "3d",
                KlineInterval.Weeks1 => "1w",
                KlineInterval.Months1 => "1M",

                _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
            };
        }
    }
}