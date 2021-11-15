using Microsoft.AspNetCore.Components;
using Orleans;
using Outcompute.Trader.Core.Timers;
using Outcompute.Trader.Trading.Algorithms;
using Polly;

namespace Outcompute.Trader.Dashboard.WebApp.Pages
{
    public sealed partial class Algos : ComponentBase, IDisposable
    {
        [Inject]
        public ILogger<Algos> Logger { get; set; } = null!;

        [Inject]
        public IGrainFactory GrainFactory { get; set; } = null!;

        [Inject]
        public ISafeTimerFactory SafeTimerFactory { get; set; } = null!;

        private IEnumerable<AlgoInfo>? _algos;

        private ISafeTimer? _timer;

        protected override async Task OnInitializedAsync()
        {
            _timer = SafeTimerFactory.Create(async token =>
            {
                await InvokeAsync(async () =>
                {
                    // query algos
                    _algos = await Policy
                        .Handle<Exception>()
                        .RetryForeverAsync(ex =>
                        {
                            LogFailedToQueryProfit(Logger, ex, nameof(Algos));
                        })
                        .ExecuteAsync(_ => GrainFactory.GetAlgoManagerGrain().GetAlgosAsync(), token, true);

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