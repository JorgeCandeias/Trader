using System.Text.Json.Serialization;

namespace Outcompute.Trader.Trading.Binance;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(MarketDataStreamRequest))]
[JsonSerializable(typeof(MarketDataStreamResult))]
internal partial class BinanceApiJsonContext : JsonSerializerContext
{
}