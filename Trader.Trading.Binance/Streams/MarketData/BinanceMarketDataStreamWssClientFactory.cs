﻿using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace Outcompute.Trader.Trading.Binance.Streams.MarketData
{
    internal class BinanceMarketDataStreamWssClientFactory : IMarketDataStreamClientFactory
    {
        private readonly IServiceProvider _provider;

        public BinanceMarketDataStreamWssClientFactory(IServiceProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public IMarketDataStreamClient Create(IReadOnlyCollection<string> streams)
        {
            return ActivatorUtilities.CreateInstance<BinanceMarketDataStreamWssClient>(_provider, streams);
        }
    }
}