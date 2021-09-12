using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace Outcompute.Trader.Hosting
{
    public interface ITraderBuilder
    {
        public ITraderBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configure);

        public ITraderBuilder ConfigureTrader(Action<HostBuilderContext, ITraderBuilder> configure);
    }
}