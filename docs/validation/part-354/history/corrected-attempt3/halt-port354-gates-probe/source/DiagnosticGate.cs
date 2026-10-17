using System.Text.Json;

namespace Erlang.Tests;

internal static class DiagnosticGate
{
    internal static void Emit(string phase, ProcessContext context)
    {
        Console.WriteLine(JsonSerializer.Serialize(new { GatePhase = phase, CancellationRequested = context.Cancellation.IsCancellationRequested, Scheduler = TaskScheduler.Current.Id, Utc = DateTimeOffset.UtcNow }));
        Console.Out.Flush();
    }

    internal static void Snapshot(Task cooperativeStopped, Task callbackEntered, Task cooperativeCompletion,
        Task callbackCompletion, ProcessContext? cooperative, ProcessContext? callback)
    {
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            GatePhase = "snapshot", CooperativeStopped = cooperativeStopped.IsCompleted,
            CallbackEntered = callbackEntered.IsCompleted, CooperativeCompletion = cooperativeCompletion.Status.ToString(),
            CallbackCompletion = callbackCompletion.Status.ToString(), CooperativeCancellation = cooperative?.Cancellation.IsCancellationRequested,
            CallbackCancellation = callback?.Cancellation.IsCancellationRequested, Utc = DateTimeOffset.UtcNow
        }));
        Console.Out.Flush();
    }
}
