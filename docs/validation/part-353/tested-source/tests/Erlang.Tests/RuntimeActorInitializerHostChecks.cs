using System.Collections;
using System.Reflection;

namespace Erlang.Tests;

internal static class RuntimeActorInitializerHostChecks
{
    internal static void RequireCurrentOwner(ProcessRuntime runtime)
    {
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        object gate = typeof(ProcessRuntime).GetField(SchedulerInitializerHostNames.Gate, flags)!.GetValue(runtime)!;
        var processes = (IDictionary)typeof(ProcessRuntime).GetField(SchedulerInitializerHostNames.Processes, flags)!.GetValue(runtime)!;
        ProcessContext initializer;
        lock (gate)
            initializer = (ProcessContext)processes[runtime.InitializationPid]!;
        TaskScheduler scheduler = TaskScheduler.Current;
        Require(!ReferenceEquals(scheduler, TaskScheduler.Default));
        Require(scheduler.MaximumConcurrencyLevel == RuntimeActorDispatchHostParameters.PerProcessConcurrency);
        ValueTask<Term> identity = runtime.Modules.Call(
            initializer,
            SchedulerAffinityHostNames.Erlang,
            SchedulerAffinityHostNames.Info,
            Term.A(SchedulerAffinityHostNames.Identity)
        );
        Require(identity.IsCompletedSuccessfully);
        Require(identity.Result is Integer lane && lane.Value >= SchedulerAffinityHostNames.First &&
            lane.Value <= runtime.ActorDispatchCapacity);
        bool entered = false;
        ValueTask nested = runtime.InitializeModulesAsync(_ =>
        {
            Require(ReferenceEquals(TaskScheduler.Current, scheduler));
            ValueTask<Term> repeated = runtime.Modules.Call(
                initializer,
                SchedulerAffinityHostNames.Erlang,
                SchedulerAffinityHostNames.Info,
                Term.A(SchedulerAffinityHostNames.Identity)
            );
            Require(repeated.IsCompletedSuccessfully && repeated.Result.Equals(identity.Result));
            entered = true;
        });
        Require(nested.IsCompletedSuccessfully && entered);
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(RuntimeActorDispatchHostNames.Failure);
    }
}
