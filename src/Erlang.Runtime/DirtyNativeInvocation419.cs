using System.Runtime.CompilerServices;
namespace Erlang;

internal sealed class DirtyNativeInvocation419
{
    private ExecutionContext? execution;
    private Func<ProcessContext, IReadOnlyList<Term>, Term>? body;
    private Term[]? arguments;
    private BeamNativeCallSite419? site;
    internal ProcessContext Owner { get; }
    internal string Module { get; }
    internal string Function { get; }
    internal TaskCompletionSource<Term> Outcome { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal TaskCompletionSource Settled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal bool Running { get; set; }
    private int physicallyExecuting423;
    internal bool PhysicallyExecuting423 => Volatile.Read(ref physicallyExecuting423) != 0;

    internal DirtyNativeInvocation419(
        ProcessContext owner,
        string module,
        string function,
        IReadOnlyList<Term> arguments,
        Func<ProcessContext, IReadOnlyList<Term>, Term> body
    )
    {
        Owner = owner;
        Module = module;
        Function = function;
        this.arguments = arguments.ToArray();
        this.body = body;
        site = owner.ScheduledNativeSite419;
        execution = ExecutionContext.Capture() ?? throw new InvalidOperationException(DirtyNativeNames419.NoContext);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void Execute(DirtyNativeKind419 kind, bool closing)
    {
        Term? value = null;
        Exception? failure = null;
        try
        {
            if (closing || Owner.Cancellation.IsCancellationRequested) throw new OperationCanceledException(Owner.Cancellation);
            ExecutionContext.Run(
                execution!,
                _ =>
            {
                // Normal actor turns are ThreadStatic, not ExecutionContext state. This
                // dedicated physical worker must never impersonate a normal dispatcher.
                if (RuntimeActorExecutionTurn.IsOwnedBy(Owner) || RuntimeActorExecutionTurn.LaneIdentity != 0
        || DirtyNativeThread419.Owner is not null || DirtyNativeThread419.Kind != DirtyNativeKind419.Normal)
                    throw new InvalidOperationException(DirtyNativeNames419.WrongThread);
                ProcessContext? previous = DirtyNativeThread419.Owner;
                DirtyNativeKind419 previousKind = DirtyNativeThread419.Kind;
                DirtyNativeThread419.Owner = Owner;
                DirtyNativeThread419.Kind = kind;
                Volatile.Write(ref physicallyExecuting423, 1);
                try
                {
                    using var mapComparison = RuntimeNumericMapComparison.EnterThread(Owner.Runtime);
                    using var scheduled = Owner.EnterScheduledNativeSource419(site);
                    try
                    {
                        value = body!(Owner, arguments!);
                    }
                    catch (ErlangException error)
                    {
                        failure = Owner.CaptureNativeFailure(
                            error,
                            Module,
                            Function,
                            arguments!
                        );
                    }
                    catch (Exception error)
                    {
                        failure = error;
                    }
                }
                finally
                {
                    Volatile.Write(ref physicallyExecuting423, 0);
                    DirtyNativeThread419.Owner = previous;
                    DirtyNativeThread419.Kind = previousKind;
                }
            },
                null
            );
        }
        catch (Exception error)
        {
            failure = error;
        }
        finally
        {
            // No worker or owner record may retain a previous invocation's source EC,
            // source-argument graph or callback after physical work has completed.
            execution = null;
            body = null;
            arguments = null;
            site = null;
        }
        if (failure is null) Outcome.TrySetResult(value!);
        else if (failure is OperationCanceledException canceled) Outcome.TrySetCanceled(canceled.CancellationToken);
        else
        {
            Outcome.TrySetException(failure);
            _ = Outcome.Task.Exception;
        }
    }

    internal void MarkSettled() => Settled.TrySetResult();

    internal void FailBeforeExecution(Exception failure)
    {
        execution = null;
        body = null;
        arguments = null;
        site = null;
        Outcome.TrySetException(failure);
        _ = Outcome.Task.Exception;
    }
}
