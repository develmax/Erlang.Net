using System.Collections;
using System.Diagnostics;
using System.Reflection;

namespace Erlang.Tests;

internal static class SchedulerInitializerHostChecks
{
    internal static async Task MailboxReadmissionAndRecursiveConfigure()
    {
        await using var runtime = new ProcessRuntime(SchedulerAffinityHostNames.Capacity);
        ProcessContext initial = Context(runtime);
        var waiting = typeof(ProcessContext).GetField(SchedulerInitializerHostNames.Waiting,
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        var watch = Stopwatch.StartNew();
        while (!(bool)waiting.GetValue(initial)!)
        {
            if (watch.Elapsed > TimeSpan.FromSeconds(SchedulerAffinityHostNames.TimeoutSeconds))
                throw new TimeoutException(SchedulerAffinityHostNames.Failure);
            await Task.Delay(SchedulerInitializerHostNames.PollMilliseconds);
        }
        bool recursive = false;
        await runtime.InitializeModulesAsync(registry =>
        {
            ValueTask<Term> identity = registry.Call(
                initial,
                SchedulerAffinityHostNames.Erlang,
                SchedulerAffinityHostNames.Info,
                Term.A(SchedulerAffinityHostNames.Identity)
            );
            Require(identity.IsCompletedSuccessfully);
            Require(identity.Result is Integer lane && lane.Value >= SchedulerAffinityHostNames.First &&
                lane.Value <= SchedulerAffinityHostNames.Capacity);
            ValueTask nested = runtime.InitializeModulesAsync(_ => recursive = true);
            Require(nested.IsCompletedSuccessfully && recursive);
        }).AsTask().WaitAsync(TimeSpan.FromSeconds(SchedulerAffinityHostNames.TimeoutSeconds));
    }

    internal static async Task RetirementWaitsForGuestThenInitializer()
    {
        await using var runtime = new ProcessRuntime(SchedulerAffinityHostNames.First);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var process = runtime.Spawn(async _ =>
        {
            entered.TrySetResult();
            await release.Task;

            return Nil.Value;
        });
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(SchedulerAffinityHostNames.TimeoutSeconds));
        var dispatcher = typeof(ProcessRuntime).GetField(SchedulerInitializerHostNames.Dispatcher,
            BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(runtime)!;
        var retired = (Task)dispatcher.GetType().GetMethod(SchedulerInitializerHostNames.Retire)!.Invoke(dispatcher, null)!;
        try
        {
            Require(!retired.IsCompleted);
        }
        finally
        {
            release.TrySetResult();
        }
        Require((await process.Completion.WaitAsync(TimeSpan.FromSeconds(SchedulerAffinityHostNames.TimeoutSeconds)))
            .Equals(Term.A(SchedulerAffinityHostNames.Normal)));
        await retired.WaitAsync(TimeSpan.FromSeconds(SchedulerAffinityHostNames.TimeoutSeconds));
    }

    private static ProcessContext Context(ProcessRuntime runtime)
    {
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        object gate = typeof(ProcessRuntime).GetField(SchedulerInitializerHostNames.Gate, flags)!.GetValue(runtime)!;
        var processes = (IDictionary)typeof(ProcessRuntime).GetField(SchedulerInitializerHostNames.Processes, flags)!.GetValue(runtime)!;
        lock (gate)
            return (ProcessContext)processes[runtime.InitializationPid]!;
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(SchedulerAffinityHostNames.Failure);
    }
}
