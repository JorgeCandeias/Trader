using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Orleans;
using Outcompute.Trader.Core.Timers;
using Outcompute.Trader.Trading.Algorithms;
using Polly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Outcompute.Trader.Dashboard.WebApp.Pages
{
    public sealed partial class Profits : ComponentBase, IDisposable
    {
        [Inject]
        public ILogger<Profits> Logger { get; set; } = null!;

        [Inject]
        public IGrainFactory GrainFactory { get; set; } = null!;

        [Inject]
        public ISafeTimerFactory SafeTimerFactory { get; set; } = null!;

        private IEnumerable<Profit>? _profits;

        private ISafeTimer? _timer;

        protected override async Task OnInitializedAsync()
        {
            _timer = SafeTimerFactory.Create(async token =>
            {
                await InvokeAsync(async () =>
                {
                    // query published profits
                    _profits = await Policy
                        .Handle<Exception>()
                        .RetryForeverAsync(ex =>
                        {
                            LogFailedToQueryProfit(Logger, ex, nameof(Profits));
                        })
                        .ExecuteAsync(_ => GrainFactory.GetProfitAggregatorGrain().GetProfitsAsync(), token, true);

                    StateHasChanged();
                });
            }, TimeSpan.Zero, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(1));

            await base.OnInitializedAsync();
        }

        public void Dispose()
        {
            _timer?.Dispose();
            GC.SuppressFinalize(this);
        }

        [LoggerMessage(0, LogLevel.Error, "{TypeName} failed to query profit")]
        private partial void LogFailedToQueryProfit(ILogger logger, Exception ex, string typeName);
    }
}