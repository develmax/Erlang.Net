namespace Erlang;

public sealed partial class ProcessRuntime
{
    private RuntimeDebugController432? debugController432;
    public RuntimeDebugController432 DebugController432
    {
        get
        {
            lock (gate)
            {
                ObjectDisposedException.ThrowIf(disposed || IsHaltRequested, this);

                return debugController432 ??= new();
            }
        }
    }

    internal async ValueTask EnterDebugBreakpoint432(
        ProcessContext context,
        ModuleCodeImage image,
        BeamLinkedFunction function,
        IReadOnlyList<Term> arguments,
        DebugBreakpointExecution432 execution
    )
    {
        if (!Modules.HasDebugBreakpoint432(image, function.EntryLabel)) return;
        RuntimeDebugStop432 stop;
        RuntimeDebugController432 owner;
        lock (gate)
        {
            RequireEtsFoundationActor(context);
            context.Cancellation.ThrowIfCancellationRequested();
            owner = debugController432 ??= new();
            stop = owner.Admit(
                context,
                image,
                function.Name,
                arguments
            );
            execution.Track(stop);
        }
        try
        {
            await context.Mailbox.WaitForControlCompletion(stop.ResumeTask, context.Cancellation);
            context.Cancellation.ThrowIfCancellationRequested();
            if (!await stop.ResumeTask) throw new ObjectDisposedException(nameof(RuntimeDebugController432));
        }
        finally
        {
            owner.ReleaseWait(stop);
        }
    }

    internal void RequireDebugDispatch432(ProcessContext context)
    {
        lock (gate)
        {
            if (!ReferenceEquals(context.Runtime, this)) throw new ArgumentException(DebugDispatchNames432.Foreign);
            RequireEtsFoundationActor(context);
            context.Cancellation.ThrowIfCancellationRequested();
        }
    }

    private Task CloseDebugDispatch432() => debugController432?.Close() ?? Task.CompletedTask;

    private Task DrainDebugDispatch432()
    {
        lock (gate) return CloseDebugDispatch432();
    }

    private bool DebugDispatchRetirementCompleted432 => debugController432 is null || debugController432.IsDrained;

    private Task DebugDispatchProcessRetirement432(ProcessContext context, Task previous)
    {
        if (debugController432 is null) return previous;
        Task own = debugController432.DrainProcess(context.Self);
        Task global = context.Self.Equals(InitializationPid) ? debugController432.Close() : Task.CompletedTask;

        return Task.WhenAll(previous, own, global);
    }
}
