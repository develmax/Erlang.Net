namespace Erlang;

internal sealed class NifImageRetirementState529(
    IAsyncDisposable? cleanup,
    Task drain,
    TaskCompletionSource completion,
    NativeResourceImageNamesRetirement529? names
)
{
    internal IAsyncDisposable? Cleanup529 { get; } = cleanup;
    internal Task Drain529 { get; } = drain;
    internal TaskCompletionSource Completion529 { get; } = completion;
    internal NativeResourceImageNamesRetirement529? Names529 { get; } = names;
    internal bool DriverStarted529 { get; set; }
}
