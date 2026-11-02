namespace Erlang;

// Only request metadata. Requester-finally retains snapshot/graph ownership.
internal sealed class PendingBinaryInfoAdmission823(
    ProcessRuntime runtime,
    ProcessContext owner,
    ProcessInfoRequest request
)
{
    internal ProcessRuntime Runtime { get; } = runtime;
    internal ProcessContext Owner { get; } = owner;
    internal ProcessInfoRequest Request { get; } = request;

    internal DeferredMailboxAdmission? Prepare() => Runtime.PrepareBinaryInfo823(this);

    internal void Release()
    {
        // No native ownership was acquired by enqueueing this control marker.
    }
}
