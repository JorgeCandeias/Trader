using System;
using Outcompute.Trader.Trading;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class UserDataStreamHostServiceCollectionExtensions
    {
        public static IServiceCollection AddUserDataStreamHost(this IServiceCollection services, Action<UserDataStreamHostOptions> configure)
        {
            return services
                .AddHostedService<UserDataStreamHost>()
                .AddOptions<UserDataStreamHostOptions>()
                .Configure(configure)
                .ValidateDataAnnotations()
                .Services;
        }
    }
}