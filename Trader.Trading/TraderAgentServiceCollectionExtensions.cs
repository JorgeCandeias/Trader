using System;
using Outcompute.Trader.Trading;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class TraderAgentServiceCollectionExtensions
    {
        public static IServiceCollection AddTraderAgent(this IServiceCollection services, Action<TraderAgentOptions> configure)
        {
            return services
                .AddHostedService<TraderAgent>()
                .AddOptions<TraderAgentOptions>()
                .Configure(configure)
                .ValidateDataAnnotations()
                .Services;
        }
    }
}