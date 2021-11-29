using System.Collections.Concurrent;
using System.Threading.Tasks.Dataflow;

namespace Outcompute.Trader.Core.Tasks.Dataflow;

/// <summary>
/// This action block batches input items on the basis of backpressure from the consuming action.
/// Each time the handler runs it receives all items available up to that point in time.
/// While the handler is running, any incoming items will be queued and then given to the next handler execution.
/// This block supports parallelism, so there can be multiple handlers running in parallel, up to <see cref="ExecutionDataflowBlockOptions.MaxDegreeOfParallelism"/>.
/// </summary>
public class BackpressureActionBlock<TInput> : ITargetBlock<TInput>
{
    private readonly ConcurrentQueue<TInput> _queue = new();

    private readonly ITargetBlock<TInput> _target;
    private readonly ITargetBlock<TInput> _action;

    public BackpressureActionBlock(Action<IEnumerable<TInput>> action)
    {
        Guard.IsNotNull(action, nameof(action));

        _target = new ActionBlock<TInput>(Enqueue);
        _action = new ActionBlock<TInput>(_ => ConflateAction(action));

        LinkCompletion();
    }

    public BackpressureActionBlock(Action<IEnumerable<TInput>> action, ExecutionDataflowBlockOptions dataflowBlockOptions)
    {
        Guard.IsNotNull(action, nameof(action));
        Guard.IsNotNull(dataflowBlockOptions, nameof(dataflowBlockOptions));

        _target = new ActionBlock<TInput>(Enqueue);
        _action = new ActionBlock<TInput>(_ => ConflateAction(action), dataflowBlockOptions);

        LinkCompletion();
    }

    public BackpressureActionBlock(Func<IEnumerable<TInput>, Task> action)
    {
        Guard.IsNotNull(action, nameof(action));

        _target = new ActionBlock<TInput>(Enqueue);
        _action = new ActionBlock<TInput>(_ => ConflateAction(action));

        LinkCompletion();
    }

    public BackpressureActionBlock(Func<IEnumerable<TInput>, Task> action, ExecutionDataflowBlockOptions dataflowBlockOptions)
    {
        Guard.IsNotNull(action, nameof(action));
        Guard.IsNotNull(dataflowBlockOptions, nameof(dataflowBlockOptions));

        _target = new ActionBlock<TInput>(Enqueue);
        _action = new ActionBlock<TInput>(_ => ConflateAction(action), dataflowBlockOptions);

        LinkCompletion();
    }

    private void LinkCompletion()
    {
        _target.Completion.ContinueWith(task => PropagateCompletion(task), TaskScheduler.Default);
    }

    private void PropagateCompletion(Task task)
    {
        // if the target block completed successfully then we complete the action block successfully
        if (task.IsCompletedSuccessfully)
        {
            // post a dummy item to ensure the action block flushes any leftovers from the queue
            _action.Post(default!);

            // complete the action block after any leftover work
            _action.Complete();

            return;
        }

        // otherwise we fault the action block as well
        _action.Fault(task.Exception!);
    }

    private void Enqueue(TInput item)
    {
        _queue.Enqueue(item);
        _action.Post(item);
    }

    private bool TryDequeue(out IEnumerable<TInput> items)
    {
        List<TInput>? _list = null;

        while (_queue.TryDequeue(out var item))
        {
            _list ??= new();

            _list.Add(item);
        }

        if (_list is null)
        {
            items = Array.Empty<TInput>();
            return false;
        }
        else
        {
            items = _list;
            return true;
        }
    }

    private void ConflateAction(Action<IEnumerable<TInput>> action)
    {
        if (TryDequeue(out var items))
        {
            action(items);
        }
    }

    private Task ConflateAction(Func<IEnumerable<TInput>, Task> action)
    {
        if (TryDequeue(out var items))
        {
            return action(items);
        }

        return Task.CompletedTask;
    }

    public Task Completion => _action.Completion;

    public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, TInput messageValue, ISourceBlock<TInput>? source, bool consumeToAccept)
    {
        return _target.OfferMessage(messageHeader, messageValue, source, consumeToAccept);
    }

    public void Complete()
    {
        _target.Complete();
    }

    public void Fault(Exception exception)
    {
        _target.Fault(exception);
    }
}