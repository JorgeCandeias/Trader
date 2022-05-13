namespace System.Threading.Tasks;

public static class TraderTaskExtensions
{
    /// <summary>
    /// Creates a task that completes when the given task completes or a timeout is reached.
    /// If the timeout is reached, the created task return the given default value, otherwise it returns the
    /// </summary>
    [Obsolete("Use WaitAsync in .NET6")]
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

    /// <inheritdoc cref="Task.WhenAll(IEnumerable{Task})"/>
    public static Task WhenAll(this IEnumerable<Task> tasks)
    {
        return Task.WhenAll(tasks);
    }

    /// <summary>
    /// Links the result of the specified task to the target task completion source.
    /// </summary>
    public static void TryLinkTo(this Task task, TaskCompletionSource taskCompletionSource)
    {
        Guard.IsNotNull(task, nameof(task));
        Guard.IsNotNull(taskCompletionSource, nameof(taskCompletionSource));

        task.ContinueWith(x =>
        {
            switch (x.Status)
            {
                case TaskStatus.RanToCompletion:
                    taskCompletionSource.TrySetResult();
                    break;

                case TaskStatus.Canceled:
                    taskCompletionSource.TrySetCanceled();
                    break;

                case TaskStatus.Faulted:
                    taskCompletionSource.TrySetException(x.Exception!);
                    break;

                default:
                    taskCompletionSource.TrySetException(new InvalidOperationException($"Unexpected {nameof(TaskStatus)} '{x.Status}'"));
                    break;
            }
        }, TaskScheduler.Default);
    }
}