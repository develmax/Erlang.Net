using System.Reflection;

namespace Erlang.Tests;

internal static class HaltElectionHostChecks
{
    internal static async Task Run()
    {
        await PendingImmediateIsFinal();
        await PendingFlushAllowsOneImmediate();
    }

    private static async Task PendingImmediateIsFinal()
    {
        await using var runtime = new ProcessRuntime();
        Task<RuntimeHaltResult> initial;
        Task<RuntimeHaltResult> later;
        lock (Gate(runtime))
        {
            initial = runtime.RequestImmediateHalt(HaltElectionHostNames.InitialStatus);
            Require(!initial.IsCompleted);
            later = runtime.RequestImmediateHalt(HaltElectionHostNames.TakeoverStatus);
            Require(ReferenceEquals(initial, later));
            Require(!initial.IsCompleted);
            Require(!runtime.HaltQuiescence.IsCompleted);
        }
        Require((await initial.WaitAsync(HaltElectionHostNames.Deadline)).Status == HaltElectionHostNames.InitialStatus);
        Require((await later.WaitAsync(HaltElectionHostNames.Deadline)).Status == HaltElectionHostNames.InitialStatus);
        await runtime.HaltQuiescence.WaitAsync(HaltElectionHostNames.Deadline);
        Require(runtime.Lifecycle == RuntimeLifecycleState.Stopped);
    }

    private static async Task PendingFlushAllowsOneImmediate()
    {
        await using var runtime = new ProcessRuntime();
        Task<RuntimeHaltResult> initial;
        Task<RuntimeHaltResult> immediate;
        Task<RuntimeHaltResult> later;
        lock (Gate(runtime))
        {
            initial = runtime.RequestHalt(HaltElectionHostNames.InitialStatus);
            Require(!initial.IsCompleted);
            immediate = runtime.RequestImmediateHalt(HaltElectionHostNames.TakeoverStatus);
            later = runtime.RequestImmediateHalt(HaltElectionHostNames.LaterStatus);
            Require(ReferenceEquals(initial, immediate) && ReferenceEquals(initial, later));
            Require(!runtime.HaltQuiescence.IsCompleted);
        }
        Require((await initial.WaitAsync(HaltElectionHostNames.Deadline)).Status == HaltElectionHostNames.TakeoverStatus);
        Require((await immediate.WaitAsync(HaltElectionHostNames.Deadline)).Status == HaltElectionHostNames.TakeoverStatus);
        Require((await later.WaitAsync(HaltElectionHostNames.Deadline)).Status == HaltElectionHostNames.TakeoverStatus);
        await runtime.HaltQuiescence.WaitAsync(HaltElectionHostNames.Deadline);
    }

    private static object Gate(ProcessRuntime runtime)
        => typeof(ProcessRuntime).GetField(HaltElectionHostNames.GateField, BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(runtime)
            ?? throw new InvalidOperationException(HaltElectionHostNames.Failure);

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(HaltElectionHostNames.Failure);
    }
}
