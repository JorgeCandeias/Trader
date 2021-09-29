using Outcompute.Trader.Dashboard;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class TraderDashboardServiceCollectionExtensions
    {
        public static IServiceCollection AddTraderDashboard(this IServiceCollection services, Action<TraderDashboardOptions> configure)
        {
            if (services is null) throw new ArgumentNullException(nameof(services));
            if (configure is null) throw new ArgumentNullException(nameof(configure));

            return services
                .AddHostedService<TraderDashboardService>()
                .AddOptions<TraderDashboardOptions>()
                .Configure(configure)
                .ValidateDataAnnotations()
                .Services;
        }
    }
}