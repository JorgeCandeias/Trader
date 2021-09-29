using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Outcompute.Trader.Dashboard.WebApp.Data;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Dashboard
{
    internal class TraderDashboardService : BackgroundService
    {
        private readonly IHost _host;

        public TraderDashboardService(IOptions<TraderDashboardOptions> options, IEnumerable<ILoggerProvider> loggerProviders)
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(web =>
                {
                    web.ConfigureLogging(logging =>
                    {
                        logging.ClearProviders();
                        foreach (var provider in loggerProviders)
                        {
                            logging.AddProvider(provider);
                        }
                    });

                    web.ConfigureServices(services =>
                    {
                        services.AddRazorPages();
                        services.AddServerSideBlazor();
                        services.AddSingleton<WeatherForecastService>();
                    });

                    web.UseStaticWebAssets();

                    web.Configure((context, app) =>
                    {
                        context.HostingEnvironment.WebRootFileProvider = new EmbeddedFileProvider(typeof(TraderDashboardService).Assembly, "wwwroot");

                        if (context.HostingEnvironment.IsDevelopment())
                        {
                            app.UseDeveloperExceptionPage();
                        }
                        else
                        {
                            app.UseExceptionHandler("/Error");
                            app.UseHsts();
                        }

                        if (options.Value.UseHttps)
                        {
                            app.UseHttpsRedirection();
                        }

                        app.UseStaticFiles();
                        app.UseRouting();

                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapBlazorHub();
                            endpoints.MapFallbackToPage("/_Host");
                        });
                    });

                    web.UseUrls($"{(options.Value.UseHttps ? "https" : "http")}://{options.Value.Host}:{options.Value.Port}");
                })
                .Build();
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await _host.StartAsync(cancellationToken).ConfigureAwait(false);

            await base.StartAsync(cancellationToken).ConfigureAwait(false);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return _host.WaitForShutdownAsync(stoppingToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await _host.StopAsync(cancellationToken).ConfigureAwait(false);

            await base.StopAsync(cancellationToken).ConfigureAwait(false);
        }

        public override void Dispose()
        {
            _host.Dispose();

            base.Dispose();
        }
    }
}