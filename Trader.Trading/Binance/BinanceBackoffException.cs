using System;
using Trader.Models;

namespace Trader.Trading.Binance
{
    public class BinanceBackoffException : Exception
    {
        public BinanceBackoffException(RateLimitType rateLimitType, TimeSpan window, double ratio)
            : base($"Rate limit usage for {rateLimitType} {window} is at {ratio}")
        {
        }
    }
}