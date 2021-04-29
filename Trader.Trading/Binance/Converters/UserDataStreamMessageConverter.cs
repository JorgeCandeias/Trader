using AutoMapper;
using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Trader.Models;
using static System.String;

namespace Trader.Trading.Binance.Converters
{
    internal class UserDataStreamMessageConverter : ITypeConverter<Memory<byte>, UserDataStreamMessage>
    {
        public UserDataStreamMessage Convert(Memory<byte> source, UserDataStreamMessage destination, ResolutionContext context)
        {
            var document = JsonDocument.Parse(source);

            var eventTypeProperty = document.RootElement.GetProperty("e");

            if (eventTypeProperty.ValueEquals("outboundAccountPosition"))
            {
                return new OutboundAccountPositionUserDataStreamMessage(
                    context.Mapper.Map<DateTime>(document.RootElement.GetProperty("E").GetInt64()),
                    context.Mapper.Map<DateTime>(document.RootElement.GetProperty("u").GetInt64()),
                    document.RootElement.GetProperty("B")
                        .EnumerateArray()
                        .Select(x => new OutboundAccountPositionBalanceUserDataStreamMessage(
                            x.GetProperty("a").GetString() ?? Empty,
                            decimal.Parse(x.GetProperty("f").GetString() ?? Empty, CultureInfo.InvariantCulture),
                            decimal.Parse(x.GetProperty("l").GetString() ?? Empty, CultureInfo.InvariantCulture)))
                        .ToImmutableList());
            }
            else if (eventTypeProperty.ValueEquals("balanceUpdate"))
            {
                return new BalanceUpdateUserDataStreamMessage(
                    context.Mapper.Map<DateTime>(document.RootElement.GetProperty("E").GetInt64()),
                    document.RootElement.GetProperty("a").GetString() ?? Empty,
                    decimal.Parse(document.RootElement.GetProperty("d").GetString() ?? Empty, CultureInfo.InvariantCulture),
                    context.Mapper.Map<DateTime>(document.RootElement.GetProperty("T").GetInt64()));
            }
            else if (eventTypeProperty.ValueEquals("executionReport"))
            {
                return new ExecutionReportUserDataStreamMessage(
                    context.Mapper.Map<DateTime>(document.RootElement.GetProperty("E").GetInt64()),
                    document.RootElement.GetProperty("s").GetString() ?? Empty,
                    document.RootElement.GetProperty("c").GetString() ?? Empty,
                    context.Mapper.Map<OrderSide>(document.RootElement.GetProperty("S").GetString()),
                    context.Mapper.Map<OrderType>(document.RootElement.GetProperty("o").GetString()),
                    context.Mapper.Map<TimeInForce>(document.RootElement.GetProperty("f").GetString()),
                    decimal.Parse(document.RootElement.GetProperty("q").GetString() ?? Empty, CultureInfo.InvariantCulture),
                    decimal.Parse(document.RootElement.GetProperty("p").GetString() ?? Empty, CultureInfo.InvariantCulture),
                    decimal.Parse(document.RootElement.GetProperty("P").GetString() ?? Empty, CultureInfo.InvariantCulture),
                    decimal.Parse(document.RootElement.GetProperty("F").GetString() ?? Empty, CultureInfo.InvariantCulture),
                    document.RootElement.GetProperty("g").GetInt64(),
                    document.RootElement.GetProperty("C").GetString() ?? Empty,
                    context.Mapper.Map<ExecutionType>(document.RootElement.GetProperty("x").GetString()),
                    context.Mapper.Map<OrderStatus>(document.RootElement.GetProperty("X").GetString()),
                    document.RootElement.GetProperty("r").GetString() ?? Empty,
                    document.RootElement.GetProperty("i").GetInt64(),
                    decimal.Parse(document.RootElement.GetProperty("l").GetString() ?? Empty, CultureInfo.InvariantCulture),
                    decimal.Parse(document.RootElement.GetProperty("z").GetString() ?? Empty, CultureInfo.InvariantCulture),
                    decimal.Parse(document.RootElement.GetProperty("L").GetString() ?? Empty, CultureInfo.InvariantCulture),
                    decimal.Parse(document.RootElement.GetProperty("n").GetString() ?? Empty, CultureInfo.InvariantCulture),
                    document.RootElement.GetProperty("N").GetString() ?? Empty,
                    context.Mapper.Map<DateTime>(document.RootElement.GetProperty("T").GetInt64()),
                    document.RootElement.GetProperty("t").GetInt64(),
                    document.RootElement.GetProperty("w").GetBoolean(),
                    document.RootElement.GetProperty("m").GetBoolean(),
                    context.Mapper.Map<DateTime>(document.RootElement.GetProperty("O").GetInt64()),
                    decimal.Parse(document.RootElement.GetProperty("Z").GetString() ?? Empty, CultureInfo.InvariantCulture),
                    decimal.Parse(document.RootElement.GetProperty("Y").GetString() ?? Empty, CultureInfo.InvariantCulture),
                    decimal.Parse(document.RootElement.GetProperty("Q").GetString() ?? Empty, CultureInfo.InvariantCulture));
            }
            else if (eventTypeProperty.ValueEquals("listStatus"))
            {
                return new ListStatusUserDataStreamMessage(
                    context.Mapper.Map<DateTime>(document.RootElement.GetProperty("E").GetInt64()),
                    document.RootElement.GetProperty("s").GetString() ?? Empty,
                    document.RootElement.GetProperty("g").GetInt64(),
                    context.Mapper.Map<ContingencyType>(document.RootElement.GetProperty("c").GetString()),
                    context.Mapper.Map<OcoStatus>(document.RootElement.GetProperty("l").GetString()),
                    context.Mapper.Map<OcoOrderStatus>(document.RootElement.GetProperty("L").GetString()),
                    document.RootElement.GetProperty("r").GetString() ?? Empty,
                    document.RootElement.GetProperty("C").GetString() ?? Empty,
                    context.Mapper.Map<DateTime>(document.RootElement.GetProperty("T").GetInt64()),
                    document.RootElement.GetProperty("O")
                        .EnumerateArray()
                        .Select(x => new ListStatusItemUserDataStreamMessage(
                            x.GetProperty("s").GetString() ?? Empty,
                            x.GetProperty("i").GetInt64(),
                            x.GetProperty("c").GetString() ?? Empty))
                        .ToImmutableList());
            }
            else
            {
                throw new AutoMapperMappingException($"Event Type '{eventTypeProperty.GetString()}' is not supported yet");
            }
        }
    }
}