namespace Erlang;

// The initializer owns cleanup until it returns this result. After return, the
// reservation owns cleanup until successful publication transfers it to the image.
internal sealed record NifImageInitialized(
    IAsyncDisposable Owner,
    IReadOnlyList<KeyValuePair<(string Name, int Arity), ErlangFunction>> Bindings
)
{
    internal bool NativePublished544 { get; set; }
    internal Exception? NativePostCommitFailure544 { get; set; }
}
