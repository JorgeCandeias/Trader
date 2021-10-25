using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace Outcompute.Trader.Trading.InMemory.MarketData
{
    internal class InMemoryMarketDataStreamClientFactory : IMarketDataStreamClientFactory
    {
        private readonly IServiceProvider _provider;

        public InMemoryMarketDataStreamClientFactory(IServiceProvider provider)
        {
            _provider = provider;
        }

        public IMarketDataStreamClient Create(IReadOnlyCollection<string> streams)
        {
            return ActivatorUtilities.CreateInstance<InMemoryMarketDataStreamClient>(_provider, streams);
        }
    }
}