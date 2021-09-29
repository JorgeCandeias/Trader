using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Dashboard;
using System;

namespace Outcompute.Trader.Hosting
{
    public static class TraderDashboardServiceTraderBuilderExtensions
    {
        public static ITraderBuilder UseTraderDashboard(this ITraderBuilder trader)
        {
            return trader.UseTraderDashboard(_ => { });
        }

        public static ITraderBuilder UseTraderDashboard(this ITraderBuilder trader, Action<TraderDashboardOptions> configure)
        {
            if (trader is null) throw new ArgumentNullException(nameof(trader));
            if (configure is null) throw new ArgumentNullException(nameof(configure));

            return trader.ConfigureServices((context, services) =>
            {
                if (!context.Properties.ContainsKey(nameof(UseTraderDashboard)))
                {
                    services.AddTraderDashboard(configure);

                    context.Properties[nameof(UseTraderDashboard)] = true;
                }
                else
                {
                    services.Configure(configure);
                }
            });
        }
    }
}