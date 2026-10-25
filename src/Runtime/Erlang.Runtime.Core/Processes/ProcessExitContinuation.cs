namespace Erlang;

internal sealed class ProcessExitContinuation(
    ProcessContext process,
    Term reason,
    IReadOnlyList<DistributionExitOutput> output
)
{
    internal ProcessContext Process { get; } = process;
    internal Term Reason { get; } = reason;
    internal IReadOnlyList<DistributionExitOutput> Output { get; } = output;
    internal TaskCompletionSource Quiescence { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal TaskCompletionSource LocalCleanup { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal Task? StorageRetirement { get; init; }
    internal List<Exception>? PreparationFailures { get; init; }
    internal int NextOutput { get; set; }
}
