using Microsoft.Extensions.DependencyInjection;
using System;

namespace Outcompute.Trader.Trading.InMemory.UserData
{
    internal class InMemoryUserDataStreamClientFactory : IUserDataStreamClientFactory
    {
        private readonly IServiceProvider _provider;

        public InMemoryUserDataStreamClientFactory(IServiceProvider provider)
        {
            _provider = provider;
        }

        public IUserDataStreamClient Create(string listenKey)
        {
            return ActivatorUtilities.CreateInstance<InMemoryUserDataStreamClient>(_provider);
        }
    }
}