namespace Outcompute.Trader.Core;

public sealed class DisposableAction : IDisposable
{
    public static readonly DisposableAction Empty = new(() => { });

    private Action? _disposeAction;

    public DisposableAction(Action disposeAction)
    {
        _disposeAction = disposeAction ?? throw new ArgumentNullException(nameof(disposeAction));
    }

    public void Dispose()
    {
        Interlocked.Exchange(ref _disposeAction, null)?.Invoke();
    }
}