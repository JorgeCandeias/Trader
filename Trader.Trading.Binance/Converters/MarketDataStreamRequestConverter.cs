using System.Text.Json;

namespace Outcompute.Trader.Trading.Binance.Converters;

internal class MarketDataStreamRequestConverter : ITypeConverter<MarketDataStreamRequest, byte[]>
{
    public byte[] Convert(MarketDataStreamRequest source, byte[] destination, ResolutionContext context)
    {
        Guard.IsNotNull(source, nameof(source));

        return JsonSerializer.SerializeToUtf8Bytes(source, BinanceApiJsonContext.Default.MarketDataStreamRequest);
    }
}