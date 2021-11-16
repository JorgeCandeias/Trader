using AutoMapper;
using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Binance.Converters;

internal class KlineIntervalConverter : ITypeConverter<KlineInterval, string>, ITypeConverter<string, KlineInterval>
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

    public KlineInterval Convert(string source, KlineInterval destination, ResolutionContext context)
    {
        return source switch
        {
            null => KlineInterval.None,

            "1m" => KlineInterval.Minutes1,
            "3m" => KlineInterval.Minutes3,
            "5m" => KlineInterval.Minutes5,
            "15m" => KlineInterval.Minutes15,
            "30m" => KlineInterval.Minutes30,
            "1h" => KlineInterval.Hours1,
            "2h" => KlineInterval.Hours2,
            "4h" => KlineInterval.Hours4,
            "6h" => KlineInterval.Hours6,
            "8h" => KlineInterval.Hours8,
            "12h" => KlineInterval.Hours12,
            "1d" => KlineInterval.Days1,
            "3d" => KlineInterval.Days3,
            "1w" => KlineInterval.Weeks1,
            "1M" => KlineInterval.Months1,

            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
        };
    }
}