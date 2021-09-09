using AutoMapper;
using System;
using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Binance.Converters
{
    internal class OrderTypeConverter : ITypeConverter<string, OrderType>, ITypeConverter<OrderType, string>
    {
        public OrderType Convert(string source, OrderType destination, ResolutionContext context)
        {
            return source switch
            {
                null => OrderType.None,

                "LIMIT" => OrderType.Limit,
                "LIMIT_MAKER" => OrderType.LimitMaker,
                "MARKET" => OrderType.Market,
                "STOP_LOSS" => OrderType.StopLoss,
                "STOP_LOSS_LIMIT" => OrderType.StopLossLimit,
                "TAKE_PROFIT" => OrderType.TakeProfit,
                "TAKE_PROFIT_LIMIT" => OrderType.TakeProfitLimit,

                _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
            };
        }

        public string Convert(OrderType source, string destination, ResolutionContext context)
        {
            return source switch
            {
                OrderType.None => null!,

                OrderType.Limit => "LIMIT",
                OrderType.LimitMaker => "LIMIT_MAKER",
                OrderType.Market => "MARKET",
                OrderType.StopLoss => "STOP_LOSS",
                OrderType.StopLossLimit => "STOP_LOSS_LIMIT",
                OrderType.TakeProfit => "TAKE_PROFIT",
                OrderType.TakeProfitLimit => "TAKE_PROFIT_LIMIT",

                _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
            };
        }
    }
}