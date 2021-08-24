using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;

namespace Trader.Hosting
{
    internal class TraderHostBuilder : ITraderHostBuilder
    {
        private readonly List<Action<HostBuilderContext, IServiceCollection>> _actions = new();

        public ITraderHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configure)
        {
            if (configure is null) throw new ArgumentNullException(nameof(configure));

            _actions.Add(configure);

            return this;
        }

        public ITraderHostBuilder ConfigureServices(Action<IServiceCollection> configure)
        {
            if (configure is null) throw new ArgumentNullException(nameof(configure));

            _actions.Add((context, services) => configure(services));

            return this;
        }

        public void Build(HostBuilderContext context, IServiceCollection services)
        {
            foreach (var action in _actions)
            {
                action(context, services);
            }

            _actions.Clear();
        }
    }
}