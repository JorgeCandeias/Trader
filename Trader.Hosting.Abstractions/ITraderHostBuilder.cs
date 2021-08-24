using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace Trader.Hosting
{
    public interface ITraderHostBuilder
    {
        public ITraderHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configure);

        public ITraderHostBuilder ConfigureServices(Action<IServiceCollection> configure);
    }
}