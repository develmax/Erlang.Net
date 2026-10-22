namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal async ValueTask<Term> ExecuteDebugDirty424(
        ProcessContext context,
        string function,
        IReadOnlyList<Term> arguments
    )
    {
        lock (gate)
        {
            if (!ReferenceEquals(context.Runtime, this)) throw new ArgumentException(DebugDirtyNames424.Foreign);
            RequireEtsFoundationActor(context);
            context.Cancellation.ThrowIfCancellationRequested();
        }
        int arity = function == DebugDirtyNames424.Dispatch ? 3 : 2;
        if (arguments.Count != arity) throw new ArgumentException(DebugDirtyNames424.Arguments);
        DirtyNativeKind419 kind;
        int first = 0;
        if (function == DebugDirtyNames424.Dispatch)
        {
            first = 1;
            kind = arguments[0] is Atom mode ? mode.Name switch
            {
                DebugDirtyNames424.Normal => DirtyNativeKind419.Normal,
                DebugDirtyNames424.Cpu => DirtyNativeKind419.Cpu,
                DebugDirtyNames424.Io => DirtyNativeKind419.Io,
                _ => throw context.CaptureNativeFailure(
                    new ErlangException(ErlangErrorReasons.BadArgument),
                    DebugDirtyNames424.Module,
                    function,
                    arguments
                )
            } : throw context.CaptureNativeFailure(
                new ErlangException(ErlangErrorReasons.BadArgument),
                DebugDirtyNames424.Module,
                function,
                arguments
            );
        }
        else kind = function == DebugDirtyNames424.Cpu ? DirtyNativeKind419.Cpu : DirtyNativeKind419.Io;
        var step = new DebugDirtyStep424(kind, arguments[first], arguments[first + 1]);
        // OTP retains the original BIF and original argv across every nested
        // schedule_bif transition (dirty_bif_SUITE multischedule_exception).
        IReadOnlyList<Term> sourceArguments = arguments;
        string sourceFunction = function;
        BeamNativeCallSite419? sourceSite = context.ScheduledNativeSite419;
        bool rescheduled = false;
        while (true)
        {
            DebugDirtyStep424 active = step;
            Term result;
            if (active.Kind == DirtyNativeKind419.Normal)
            {
                var scheduler = context.ActorTaskScheduler ?? throw new InvalidOperationException(DebugDirtyNames424.NormalScheduler);
                // Queue an actual normal actor turn, including after dirty IO.
                // Task.Run/ambient scheduler flags cannot implement this edge.
                string currentFunction = sourceFunction;
                IReadOnlyList<Term> currentArguments = sourceArguments;
                bool scheduledContinuation = rescheduled;
                result = await scheduler.Invoke(() =>
                {
                    lock (gate)
                    {
                        RequireEtsFoundationActor(context);
                        context.Cancellation.ThrowIfCancellationRequested();
                    }
                    if (!RuntimeActorExecutionTurn.IsOwnedBy(context)) throw new InvalidOperationException(DebugDirtyNames424.NormalScheduler);
                    using var scheduled = scheduledContinuation ? context.EnterScheduledNativeSource419(sourceSite) : null;
                    try
                    {
                        return ValueTask.FromResult(DebugDirtyOperations424.Execute(this, context, active));
                    }
                    catch (ErlangException error)
                    {
                        throw context.CaptureNativeFailure(
                            error,
                            DebugDirtyNames424.Module,
                            currentFunction,
                            currentArguments
                        );
                    }
                }).ConfigureAwait(false);
            }
            else
            {
                result = await ScheduleDirtyNative419(
                    context,
                    active.Kind,
                    DebugDirtyNames424.Module,
                    sourceFunction,
                    sourceArguments,
                    (owner, _) => DebugDirtyOperations424.Execute(this, owner, active)
                ).ConfigureAwait(false);
            }
            if (active.Next is null) return result;
            step = active.Next;
            rescheduled = true;
        }
    }

    internal bool DebugDirtyExiting424(ProcessContext context)
    {
        lock (gate) return context.Terminated || context.Cancellation.IsCancellationRequested;
    }

    internal void DebugDirtySend424(ProcessContext source, Term target, string tag)
    {
        lock (gate)
        {
            if (target is Pid pid && ReferenceEquals(pid.LocalIdentity, LocalIdentity)
                && processes.TryGetValue(pid, out var receiver) && !receiver.Terminated)
                receiver.Mailbox.SendTraced(
                    Term.Tuple(Term.A(tag), source.Self),
                    null,
                    preserve: false,
                    from: source.Self
                );
        }
    }
}
