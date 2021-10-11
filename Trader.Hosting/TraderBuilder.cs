using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;

namespace Outcompute.Trader.Hosting
{
    [Obsolete("Remove")] // todo: remove this class
    internal class TraderBuilder : ITraderBuilder
    {
        private readonly List<Action<HostBuilderContext, ITraderBuilder>> _traderActions = new();
        private readonly List<Action<HostBuilderContext, IServiceCollection>> _serviceActions = new();

        public ITraderBuilder ConfigureTrader(Action<HostBuilderContext, ITraderBuilder> configure)
        {
            if (configure is null) throw new ArgumentNullException(nameof(configure));

            _traderActions.Add(configure);

            return this;
        }

        public ITraderBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configure)
        {
            if (configure is null) throw new ArgumentNullException(nameof(configure));

            _serviceActions.Add(configure);

            return this;
        }

        public void Build(HostBuilderContext context, IServiceCollection services)
        {
            foreach (var action in _traderActions)
            {
                action(context, this);
            }

            foreach (var action in _serviceActions)
            {
                action(context, services);
            }
        }
    }
}