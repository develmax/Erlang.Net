namespace Erlang;

internal sealed class ProcessExitContinuation(
    ProcessContext process,
    Term reason,
    IReadOnlyList<DistributionExitOutput> output
)
{
    internal ProcessContext Process { get; } = process;
    internal PreparedSequenceTermination? Sequence641 { get; set; }
    internal RuntimeProcessPayload641? Origin641 { get; set; }
    internal Term Reason { get; } = reason;
    internal IReadOnlyList<DistributionExitOutput> Output { get; } = output;
    internal TaskCompletionSource Quiescence { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal TaskCompletionSource LocalCleanup { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal Task? StorageRetirement { get; init; }
    internal Task PhysicalRetirement { get; init; } = Task.CompletedTask;
    internal List<Exception>? PreparationFailures { get; init; }
    internal int NextOutput { get; set; }
}
