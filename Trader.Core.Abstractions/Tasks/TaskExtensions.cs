namespace System.Threading.Tasks
{
    public static class TaskExtensions
    {
        /// <summary>
        /// Creates a task that completes when the given task completes or a timeout is reached.
        /// If the timeout is reached, the created task return the given default value, otherwise it returns the
        /// </summary>
        public static Task<TResult> WithDefaultOnTimeout<TResult>(this Task<TResult> task, TResult defaultValue, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            if (task == null) throw new ArgumentNullException(nameof(task));
            if (timeout < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));

            // quick path for completed task
            if (task.IsCompleted) return task;

            // quick path for infinite timeout
            if (timeout == TimeSpan.MaxValue) return task;

            // quick path for zero timeout
            if (timeout == TimeSpan.Zero) return Task.FromResult(defaultValue);

            // slow path for regular completion
            var delay = Task.Delay(timeout, cancellationToken).ContinueWith((_, _) => defaultValue, null, cancellationToken, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            return Task.WhenAny(task, delay).Unwrap();
        }
    }
}