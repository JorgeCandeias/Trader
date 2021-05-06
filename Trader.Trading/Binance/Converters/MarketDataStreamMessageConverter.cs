using AutoMapper;
using System;
using System.Text.Json;
using Trader.Models;

namespace Trader.Trading.Binance.Converters
{
    internal class MarketDataStreamMessageConverter : ITypeConverter<Memory<byte>, MarketDataStreamMessage>
    {
        public MarketDataStreamMessage Convert(Memory<byte> source, MarketDataStreamMessage destination, ResolutionContext context)
        {
            using var document = JsonDocument.Parse(source);

            // attempt to parse an error message
            if (document.RootElement.TryGetProperty("code", out var codeProperty) && codeProperty.TryGetInt32(out var codeValue) &&
                document.RootElement.TryGetProperty("msg", out var messageProperty) && messageProperty.ValueKind is JsonValueKind.String)
            {
                var error = new ExternalError(codeValue, messageProperty.GetRequiredString());

                return new MarketDataStreamMessage(error, null);
            }

            // see if we got a composite stream message
            if (document.RootElement.TryGetProperty("stream", out var streamProperty) && streamProperty.ValueKind == JsonValueKind.String)
            {
                if (!document.RootElement.TryGetProperty("data", out var dataProperty)) throw new AutoMapperMappingException($"Unknown composite message '{document}'");

                return ConvertSingleMessage(dataProperty, context);
            }

            // otherwise convert from a regular stream message
            return ConvertSingleMessage(document.RootElement, context);
        }

        private static MarketDataStreamMessage ConvertSingleMessage(JsonElement element, ResolutionContext context)
        {
            // attempt to parse regular messages
            if (!element.TryGetProperty("e", out var eventTypeProperty)) throw new AutoMapperMappingException($"Unknow Message '{element}'");

            // attempt to parse a 24h mini ticker message
            if (eventTypeProperty.ValueEquals("24hrMiniTicker"))
            {
                var ticker = new MiniTicker(
                    element.GetProperty("s").GetRequiredString(),
                    context.Mapper.Map<DateTime>(element.GetProperty("E").GetInt64()),
                    element.GetProperty("c").GetRequiredDecimalFromString(),
                    element.GetProperty("o").GetRequiredDecimalFromString(),
                    element.GetProperty("h").GetRequiredDecimalFromString(),
                    element.GetProperty("l").GetRequiredDecimalFromString(),
                    element.GetProperty("v").GetRequiredDecimalFromString(),
                    element.GetProperty("q").GetRequiredDecimalFromString());

                return new MarketDataStreamMessage(null, ticker);
            }

            // return an empty message if we cant detect the message type
            return new MarketDataStreamMessage(null, null);
        }
    }
}