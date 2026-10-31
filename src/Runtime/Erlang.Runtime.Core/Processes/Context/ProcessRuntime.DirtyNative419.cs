namespace Erlang;

public sealed partial class ProcessRuntime
{
    private DirtyNativeOwner419? dirtyNative419;

    internal ValueTask<Term> ScheduleDirtyNative419(
        ProcessContext context,
        DirtyNativeKind419 kind,
        string module,
        string function,
        IReadOnlyList<Term> arguments,
        Func<ProcessContext, IReadOnlyList<Term>, Term> body,
        Action<Task>? physicalSettlement521 = null
    )
    {
        DirtyNativeOwner419 owner;
        lock (gate)
        {
            if (!ReferenceEquals(context.Runtime, this)) throw new ArgumentException(DirtyNativeNames419.Foreign, nameof(context));
            RequireEtsFoundationActor(context);
            context.Cancellation.ThrowIfCancellationRequested();
            owner = dirtyNative419 ??= new(dirtyIoSchedulerCount522, Statistics.Microstates);
        }

        var scheduler = context.ActorTaskScheduler ?? throw new InvalidOperationException(DirtyNativeNames419.NoContext);
        Task<Term> pending = owner.Enqueue(
            context,
            kind,
            module,
            function,
            arguments,
            body,
            physicalSettlement521
        );

        // The physical callback stays on its owned CPU/IO worker. Its process
        // must still admit ordered control signals on an actual normal turn.
        // In particular, exit cannot wait for an alive_waitexiting body to return.
        return new(scheduler.Invoke(async () =>
        {
            await context.Mailbox.WaitForControlCompletion(pending, context.Cancellation);

            return await pending;
        }));
    }

    private Task CloseDirtyNative419() => dirtyNative419?.BeginClose() ?? Task.CompletedTask;

    private Task DrainDirtyNative419()
    {
        lock (gate) return CloseDirtyNative419();
    }

    private bool DirtyNativeRetirementCompleted419 => dirtyNative419 is null || dirtyNative419.Completion.IsCompletedSuccessfully;

    private Task DirtyNativeProcessRetirement419(ProcessContext context, Task previous)
    {
        if (dirtyNative419 is null) return previous;
        Task own = dirtyNative419.DrainProcess(context);
        Task global = context.Self.Equals(InitializationPid) ? dirtyNative419.BeginClose() : Task.CompletedTask;

        return Task.WhenAll(previous, own, global);
    }
}
