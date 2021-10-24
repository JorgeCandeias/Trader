using Microsoft.Extensions.DependencyInjection;
using System;

namespace Outcompute.Trader.Core
{
    public class NullServiceProvider : IServiceProvider
    {
        private NullServiceProvider()
        {
        }

        private readonly IServiceProvider _provider = new ServiceCollection().BuildServiceProvider();

        public object? GetService(Type serviceType)
        {
            return _provider.GetService(serviceType);
        }

        public static NullServiceProvider Instance { get; } = new NullServiceProvider();
    }
}