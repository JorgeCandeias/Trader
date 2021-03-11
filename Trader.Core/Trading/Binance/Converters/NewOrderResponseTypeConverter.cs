﻿using AutoMapper;
using System;

namespace Trader.Core.Trading.Binance.Converters
{
    internal class NewOrderResponseTypeConverter : ITypeConverter<NewOrderResponseType, string>
    {
        public string Convert(NewOrderResponseType source, string destination, ResolutionContext context)
        {
            return source switch
            {
                NewOrderResponseType.None => null!,

                NewOrderResponseType.Acknowledge => "ACK",
                NewOrderResponseType.Result => "RESULT",
                NewOrderResponseType.Full => "FULL",

                _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
            };
        }
    }
}