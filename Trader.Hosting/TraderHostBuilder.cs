using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;

namespace Trader.Hosting
{
    internal class TraderHostBuilder : ITraderHostBuilder
    {
        private readonly List<Action<HostBuilderContext, ITraderHostBuilder>> _traderActions = new();
        private readonly List<Action<HostBuilderContext, IServiceCollection>> _serviceActions = new();

        public ITraderHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configure)
        {
            if (configure is null) throw new ArgumentNullException(nameof(configure));

            _serviceActions.Add(configure);

            return this;
        }

        public ITraderHostBuilder ConfigureTrader(Action<HostBuilderContext, ITraderHostBuilder> configure)
        {
            if (configure is null) throw new ArgumentNullException(nameof(configure));

            _traderActions.Add(configure);

            return this;
        }

        public void Build(HostBuilderContext context, IServiceCollection services)
        {
            foreach (var action in _traderActions)
            {
                action(context, this);
            }

            this.AddTraderCore();

            foreach (var action in _serviceActions)
            {
                action(context, services);
            }
        }
    }
}