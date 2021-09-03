using AutoMapper;
using System;
using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Binance.Converters
{
    internal class OrderSideConverter : ITypeConverter<string, OrderSide>, ITypeConverter<OrderSide, string>
    {
        public OrderSide Convert(string source, OrderSide destination, ResolutionContext context)
        {
            return source switch
            {
                null => OrderSide.None,

                "BUY" => OrderSide.Buy,
                "SELL" => OrderSide.Sell,

                _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
            };
        }

        public string Convert(OrderSide source, string destination, ResolutionContext context)
        {
            return source switch
            {
                OrderSide.None => null!,

                OrderSide.Buy => "BUY",
                OrderSide.Sell => "SELL",

                _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
            };
        }
    }
}