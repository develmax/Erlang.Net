namespace Erlang;

internal sealed class NativeDriverSignal454(
    Pid? caller,
    bool waitsForBusy,
    Func<Task<Term>> execute
)
{
    internal Pid? Caller { get; } = caller;
    internal bool WaitsForBusy { get; } = waitsForBusy;
    internal Func<Task<Term>> Execute { get; } = execute;
    internal TaskCompletionSource<Term> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}
