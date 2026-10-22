using System.Runtime.InteropServices;

namespace Erlang;

internal sealed class PrimTtyResourceState(PrimTtyRedirectedHandleScope handles)
{
    private readonly object gate = new();
    private readonly CancellationTokenSource stop = new();
    private readonly HashSet<Task> selections = new();
    private readonly List<Exception> failures = new();
    private CancellationTokenSource? selected;
    private int active;
    private bool closed;
    private TaskCompletionSource? drained;
    private Task? retirement;
    internal PrimTtyRedirectedHandleScope Handles { get; } = handles;
    internal CancellationToken Stop => stop.Token;

    internal IDisposable Enter()
    {
        lock (gate)
        {
            if (closed)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            active++;

            return new Operation(this);
        }
    }

    private sealed class Operation(PrimTtyResourceState owner) : IDisposable
    {
        private PrimTtyResourceState? current = owner;

        public void Dispose()
        {
            var target = Interlocked.Exchange(ref current, null);
            if (target is null) return;
            lock (target.gate)
            {
                if (--target.active == 0) target.drained?.TrySetResult();
            }
        }
    }

    internal Task BeginClose()
    {
        TaskCompletionSource completion;
        lock (gate)
        {
            if (retirement is not null) return retirement;
            closed = true;
            completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            retirement = completion.Task;
        }
        _ = FinishClose(completion);

        return completion.Task;
    }

    private async Task FinishClose(TaskCompletionSource completion)
    {
        var errors = new List<Exception>();
        try
        {
            stop.Cancel();
        }
        catch (Exception error)
        {
            errors.Add(error);
        }
        Task calls;
        lock (gate)
            calls = active == 0 ? Task.CompletedTask
                : (drained ??= new(TaskCreationOptions.RunContinuationsAsynchronously)).Task;
        try
        {
            await calls.ConfigureAwait(false);
        }
        catch (Exception error)
        {
            errors.Add(error);
        }
        Task[] pending;
        lock (gate) pending = selections.ToArray();
        try
        {
            await Task.WhenAll(pending).ConfigureAwait(false);
        }
        catch (Exception error)
        {
            errors.Add(error);
        }
        try
        {
            Handles.Dispose();
        }
        catch (Exception error)
        {
            errors.Add(error);
        }
        try
        {
            stop.Dispose();
        }
        catch (Exception error)
        {
            errors.Add(error);
        }
        lock (gate) errors.AddRange(failures);
        if (errors.Count == 0) completion.TrySetResult();
        else completion.TrySetException(new AggregateException(errors.Distinct()));
    }

    internal void Select(ProcessContext context, ResourceReferenceTerm reference, Term token)
    {
        CancellationTokenSource replacement;
        CancellationTokenSource? previous;
        Task? completion = null;
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (gate)
        {
            if (closed) throw new ErlangException(ErlangErrorReasons.BadArgument);
            replacement = CancellationTokenSource.CreateLinkedTokenSource(stop.Token, context.Cancellation);
            previous = selected;
            selected = replacement;
            Task loop = StartSelection(
                start.Task,
                context.Runtime,
                context.Self,
                reference,
                token,
                replacement
            );
            completion = loop.ContinueWith(
                done =>
            {
                var errors = new List<Exception>();
                if (done.IsFaulted) errors.Add(done.Exception!);
                try
                {
                    replacement.Dispose();
                }
                catch (Exception error)
                {
                    errors.Add(error);
                }
                lock (gate)
                {
                    selections.Remove(completion!);
                    failures.AddRange(errors);
                    if (ReferenceEquals(selected, replacement)) selected = null;
                }
                if (errors.Count != 0) throw new AggregateException(errors);
            },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default
            );
            selections.Add(completion);
        }
        try
        {
            previous?.Cancel();
        }
        catch (ObjectDisposedException) { /* Previous selection has already settled. */ }
        finally
        {
            start.TrySetResult();
        }
    }

    private async Task StartSelection(
        Task start,
        ProcessRuntime runtime,
        Pid reader,
        ResourceReferenceTerm reference,
        Term token,
        CancellationTokenSource selection
    )
    {
        await start.ConfigureAwait(false);
        await SelectionLoop(
            runtime,
            reader,
            reference,
            token,
            selection
        ).ConfigureAwait(false);
    }

    private async Task SelectionLoop(
        ProcessRuntime runtime,
        Pid reader,
        ResourceReferenceTerm reference,
        Term token,
        CancellationTokenSource selection
    )
    {
        try
        {
            while (true)
            {
                selection.Token.ThrowIfCancellationRequested();
                nint handle = Handles.InputHandle;
                uint kind = PrimTtyWindowsNative.GetFileType(handle);
                bool ready = kind == PrimTtyWindowsNames.TypeDisk;
                if (kind == PrimTtyWindowsNames.TypePipe)
                {
                    bool open = PrimTtyWindowsNative.PeekNamedPipe(
                        handle,
                        0,
                        0,
                        0,
                        out uint count,
                        0
                    );
                    ready = open ? count != 0 : (uint)Marshal.GetLastPInvokeError() == PrimTtyWindowsNames.BrokenPipe;
                }
                if (ready)
                {
                    lock (gate)
                        if (closed || !ReferenceEquals(selected, selection)) return;
                    runtime.SendSystem(reader, Term.Tuple(
                        PrimTtyAtoms.Select,
                        reference,
                        token,
                        PrimTtyAtoms.ReadyInput
                    ));

                    return;
                }
                await Task.Delay(PrimTtyNames.PollMilliseconds, selection.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (selection.IsCancellationRequested) { }
    }
}
