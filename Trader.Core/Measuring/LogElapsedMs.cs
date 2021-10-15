using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Logging
{
    public static class ILoggerMeasuringExtensions
    {
        public static ValueTask LogElapsedMsAsync(this ILogger logger, string name, Func<ValueTask> action)
        {
            if (action is null) throw new ArgumentNullException(nameof(action));

            return logger.LogElapsedMsInnerAsync(name, action);
        }

        private static async ValueTask LogElapsedMsInnerAsync(this ILogger logger, string name, Func<ValueTask> action)
        {
            var watch = Stopwatch.StartNew();

            await action().ConfigureAwait(false);

            watch.Stop();

            logger.LogElapsedMs(name, watch.ElapsedMilliseconds);
        }

        private static readonly Action<ILogger, string, long, Exception> _logElapsedMs =
            LoggerMessage.Define<string, long>(LogLevel.Information, new EventId(0, nameof(LogElapsedMs)), "Task {Name} completed in {ElapsedMs}");

        private static void LogElapsedMs(this ILogger logger, string name, long elapsedMs) =>
            _logElapsedMs(logger, name, elapsedMs, null!);
    }
}