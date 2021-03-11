﻿using System;
using System.Runtime.Serialization;

namespace Trader.Core.Trading.Binance
{
    [Serializable]
    public class BinanceUnknownResponseException : BinanceException
    {
        public BinanceUnknownResponseException()
        {
        }

        public BinanceUnknownResponseException(string? message) : base(message)
        {
        }

        public BinanceUnknownResponseException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected BinanceUnknownResponseException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}