namespace Erlang;

// Symmetric control requests allow a native driver callback to ask the main VM
// about its actual process while that VM awaits the invoking driver operation.
// Output has its own pipe, so output backpressure never blocks control responses.
internal sealed class ErlDriverHostPeer420 : IAsyncDisposable
{
    private readonly object gate = new();
    private readonly ErlDriverHostPipe420 control;
    private readonly ErlDriverHostPipe420 events;
    private readonly string session;
    private readonly bool eventSender;
    private readonly Func<ErlDriverHostFrame420, CancellationToken, ValueTask<ErlDriverHostFrame420>> onRequest;
    private readonly Func<ErlDriverHostFrame420, CancellationToken, ValueTask> onEvent;
    private readonly CancellationTokenSource stopped = new();
    private readonly Dictionary<ulong, ErlDriverHostPending420> pending = new();
    private readonly HashSet<ulong> incoming = new();
    private readonly HashSet<Task> handlers = new();
    private readonly Task controlReader;
    private readonly Task eventReader;
    private readonly TaskCompletionSource start = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource eventDrain = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly SemaphoreSlim eventWrites = new(1, 1);
    private bool eventWritesSealed;
    private Exception? failure;
    private ulong nextRequest;
    private ulong sentEvents;
    private ulong admittedEvents;
    private TaskCompletionSource eventProgress = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal Task EventsDrained => eventDrain.Task;
    internal Task Completion => controlReader;

    internal ErlDriverHostPeer420(
        ErlDriverHostPipe420 control,
        ErlDriverHostPipe420 events,
        string session,
        bool eventSender,
        Func<ErlDriverHostFrame420, CancellationToken, ValueTask<ErlDriverHostFrame420>> onRequest,
        Func<ErlDriverHostFrame420, CancellationToken, ValueTask> onEvent
    )
    {
        this.control = control;
        this.events = events;
        this.session = session;
        this.eventSender = eventSender;
        this.onRequest = onRequest;
        this.onEvent = onEvent;
        bool suppress = !ExecutionContext.IsFlowSuppressed();
        AsyncFlowControl flow = default;
        try
        {
            if (suppress) flow = ExecutionContext.SuppressFlow();
            controlReader = ReadControl();
            eventReader = eventSender ? Task.CompletedTask : ReadEvents();
        }
        finally
        {
            if (suppress) flow.Undo();
        }
        start.TrySetResult();
    }

    internal async ValueTask<ErlDriverHostFrame420> RequestAsync(
        ErlDriverHostOperation420 operation,
        ulong driver,
        ulong port,
        ulong actor,
        uint command,
        byte[]? name,
        byte[]? data,
        CancellationToken cancellation,
        bool schedulerThread = false
    )
    {
        cancellation.ThrowIfCancellationRequested();
        var completion = new TaskCompletionSource<ErlDriverHostFrame420>(TaskCreationOptions.RunContinuationsAsynchronously);
        ulong id;
        ErlDriverHostFrame420 frame;
        lock (gate)
        {
            ThrowIfClosed();
            id = checked(nextRequest + 1);
            frame = new(
                ErlDriverHostNames420.Version,
                session,
                ErlDriverHostKind420.Request,
                operation,
                id,
                driver,
                port,
                actor,
                command,
                0,
                name?.ToArray(),
                data?.ToArray(),
                eventSender ? sentEvents : 0,
                schedulerThread
            );
            pending.Add(id, new(
                operation,
                driver,
                port,
                actor,
                command,
                schedulerThread,
                completion
            ));
            nextRequest = id;
        }
        try
        {
            // Once admitted, wait for the genuine result. Abandoning a response
            // on actor cancellation could leak an already-created native port.
            await control.WriteAsync(frame, stopped.Token).ConfigureAwait(false);
        }
        catch (Exception error)
        {
            await Fail(error).ConfigureAwait(false);
        }

        return await completion.Task.ConfigureAwait(false);
    }

    internal async ValueTask SendEventAsync(ErlDriverHostFrame420 frame)
    {
        await eventWrites.WaitAsync(stopped.Token).ConfigureAwait(false);
        try
        {
            lock (gate)
            {
                ThrowIfClosed();
                if (!eventSender || eventWritesSealed || frame.Kind != ErlDriverHostKind420.Event || frame.Request != 0
                    || frame.Operation == ErlDriverHostOperation420.Shutdown)
                    throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
            }
            ulong sequence;
            lock (gate) sequence = checked(sentEvents + 1);
            await events.WriteAsync(frame with { EventSequence = sequence }, stopped.Token).ConfigureAwait(false);
            lock (gate) sentEvents = sequence;
        }
        catch (Exception error)
        {
            await Fail(error).ConfigureAwait(false);
            throw;
        }
        finally
        {
            eventWrites.Release();
        }
    }

    internal async ValueTask SealEventsAsync()
    {
        await eventWrites.WaitAsync(stopped.Token).ConfigureAwait(false);
        try
        {
            lock (gate)
            {
                ThrowIfClosed();
                if (!eventSender || eventWritesSealed) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
                eventWritesSealed = true;
            }
            ulong sequence;
            lock (gate) sequence = checked(sentEvents + 1);
            await events.WriteAsync(
                new(
                    ErlDriverHostNames420.Version,
                    session,
                    ErlDriverHostKind420.Event,
                    ErlDriverHostOperation420.Shutdown,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    null,
                    null,
                    sequence
                ),
                stopped.Token
            ).ConfigureAwait(false);
            lock (gate) sentEvents = sequence;
            eventDrain.TrySetResult();
        }
        catch (Exception error)
        {
            await Fail(error).ConfigureAwait(false);
            throw;
        }
        finally
        {
            eventWrites.Release();
        }
    }

    private async Task ReadControl()
    {
        await start.Task.ConfigureAwait(false);
        try
        {
            while (true)
            {
                var frame = await control.ReadAsync(stopped.Token).ConfigureAwait(false);
                if (frame.Request == 0) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
                if (frame.Kind == ErlDriverHostKind420.Response)
                {
                    ErlDriverHostPending420 request;
                    lock (gate)
                    {
                        if (!pending.TryGetValue(frame.Request, out request!) || request.Operation != frame.Operation
                            || request.Driver != frame.Driver || request.Port != frame.Port || request.Actor != frame.Actor
                            || request.Command != frame.Command || request.SchedulerThread != frame.SchedulerThread || request.Reply is not null || eventSender && frame.EventSequence != 0)
                            throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
                        request.Reply = frame;
                        CompleteReplies();
                    }
                }
                else if (frame.Kind == ErlDriverHostKind420.Request)
                {
                    if (eventSender && frame.EventSequence != 0) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
                    lock (gate)
                    {
                        ThrowIfClosed();
                        if (!incoming.Add(frame.Request)) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
                        // Never run guest/runtime work synchronously on this
                        // response reader: callbacks can issue nested requests.
                        Task handler = Task.Factory.StartNew(
                            () => Handle(frame),
                            CancellationToken.None,
                            TaskCreationOptions.DenyChildAttach,
                            TaskScheduler.Default
                        ).Unwrap();
                        handlers.Add(handler);
                        _ = handler.ContinueWith(
                            RemoveHandler,
                            CancellationToken.None,
                            TaskContinuationOptions.None,
                            TaskScheduler.Default
                        );
                    }
                }
                else throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
            }
        }
        catch (Exception error)
        {
            await Fail(error).ConfigureAwait(false);
        }
    }

    private void RemoveHandler(Task task)
    {
        lock (gate) handlers.Remove(task);
    }

    private async Task Handle(ErlDriverHostFrame420 frame)
    {
        try
        {
            await WaitEventPrefix(frame.EventSequence).ConfigureAwait(false);
            var reply = await onRequest(frame, stopped.Token).ConfigureAwait(false);
            if (reply.Kind != ErlDriverHostKind420.Response || reply.Operation != frame.Operation || reply.Request != frame.Request)
                throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
            lock (gate) reply = reply with { EventSequence = eventSender ? sentEvents : 0 };
            await control.WriteAsync(reply, stopped.Token).ConfigureAwait(false);
        }
        catch (Exception error)
        {
            await Fail(error).ConfigureAwait(false);
        }
        finally
        {
            lock (gate) incoming.Remove(frame.Request);
        }
    }

    private async Task ReadEvents()
    {
        await start.Task.ConfigureAwait(false);
        try
        {
            while (true)
            {
                var frame = await events.ReadAsync(stopped.Token).ConfigureAwait(false);
                if (frame.Kind != ErlDriverHostKind420.Event || frame.Request != 0)
                    throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
                lock (gate)
                    if (frame.EventSequence != checked(admittedEvents + 1))
                        throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
                if (frame.Operation == ErlDriverHostOperation420.Shutdown)
                {
                    if (frame.Driver != 0 || frame.Port != 0 || frame.Actor != 0 || frame.Command != 0
                        || frame.NativeResult != 0 || frame.Name is not null || frame.Data is not null)
                        throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
                    AdmitEvent(frame.EventSequence);
                    eventDrain.TrySetResult();

                    return;
                }
                await onEvent(frame, stopped.Token).ConfigureAwait(false);
                AdmitEvent(frame.EventSequence);
            }
        }
        catch (Exception error)
        {
            await Fail(error).ConfigureAwait(false);
        }
    }

    private void ThrowIfClosed()
    {
        if (failure is not null) throw new IOException(ErlDriverHostNames420.Closed, failure);
    }

    private void AdmitEvent(ulong sequence)
    {
        lock (gate)
        {
            admittedEvents = sequence;
            var previous = eventProgress;
            eventProgress = new(TaskCreationOptions.RunContinuationsAsynchronously);
            previous.TrySetResult();
            CompleteReplies();
        }
    }

    private async Task WaitEventPrefix(ulong sequence)
    {
        while (true)
        {
            Task changed;
            lock (gate)
            {
                ThrowIfClosed();
                if (sequence <= admittedEvents) return;
                changed = eventProgress.Task;
            }
            await changed.ConfigureAwait(false);
        }
    }

    private void CompleteReplies()
    {
        // Keep a response in pending ownership until its actual event prefix
        // has been admitted. A later pipe failure must settle these calls too.
        ulong[] ready = pending.Where(row => row.Value.Reply is { } reply
            && reply.EventSequence <= admittedEvents).Select(row => row.Key).ToArray();
        foreach (ulong id in ready)
        {
            var call = pending[id];
            pending.Remove(id);
            call.Completion.TrySetResult(call.Reply!);
        }
    }

    private async ValueTask Fail(Exception error)
    {
        lock (gate)
        {
            if (failure is not null) return;
            failure = error;
            eventProgress.TrySetResult();
            eventDrain.TrySetException(error);
            // RunContinuationsAsynchronously guarantees no caller executes
            // under this lock; do not allocate a post-failure snapshot.
            foreach (var call in pending.Values) call.Completion.TrySetException(error);
            pending.Clear();
        }
        // Settle the actual outstanding native invocations even if transport
        // disposal itself fails. The owning process still joins child exit.
        try
        {
            stopped.Cancel();
        }
        finally
        {
            try
            {
                await control.DisposeAsync().ConfigureAwait(false);
            }
            finally
            {
                await events.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    internal ValueTask AbortAsync(Exception error) => Fail(error);

    public async ValueTask DisposeAsync()
    {
        var failures = new List<Exception>();
        try
        {
            await Fail(new ObjectDisposedException(ErlDriverHostNames420.Closed)).ConfigureAwait(false);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        try
        {
            await Task.WhenAll(controlReader, eventReader).ConfigureAwait(false);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        Task[] activeHandlers;
        lock (gate) activeHandlers = handlers.ToArray();
        try
        {
            await Task.WhenAll(activeHandlers).ConfigureAwait(false);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        if (failures.Count != 0) throw new AggregateException(failures);
    }
}
