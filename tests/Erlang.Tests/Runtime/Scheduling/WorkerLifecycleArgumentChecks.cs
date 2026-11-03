using static Erlang.Tests.WorkerLifecycleAccess;

namespace Erlang.Tests;

internal static class WorkerLifecycleArgumentChecks
{
    internal static Task Run()
    {
        object? retained = null;
        object policy = Policy(checkpoint: (phase, runtime) =>
        {
            if (phase == WorkerLifecycleNames.PrefixBeforeAtoms)
                retained = runtime;
        });
        Exception? actual = null;
        try
        {
            _ = Create(
                typeof(ProcessRuntime).FullName!,
                new string(WorkerLifecycleArgumentNames.NodeCharacter, WorkerLifecycleArgumentNames.OverlongNodeCodePoints),
                null,
                WorkerLifecycleArgumentNames.ProcessCapacity,
                null,
                WorkerLifecycleArgumentNames.NormalWorkers,
                new ProcessRuntimeOptions { DirtyIoSchedulerCount = WorkerLifecycleArgumentNames.DirtyIoWorkers },
                policy
            );
        }
        catch (Exception error)
        {
            actual = error;
        }
        if (actual is not ErlangException { ExceptionClass: ErlangExceptionClasses.Error } erlang
            || !erlang.Reason.Equals(Term.A(ErlangErrorReasons.SystemLimit)))
            throw new InvalidOperationException(WorkerLifecycleNames.MissingMarker + nameof(ErlangException), actual);
        Require(retained is not null);
        RuntimeResources(retained!);

        return Task.CompletedTask;
    }
}
