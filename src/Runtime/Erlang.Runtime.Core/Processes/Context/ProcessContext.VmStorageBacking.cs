namespace Erlang;

public sealed partial class ProcessContext
{
    // ProcessContext is created only by trusted runtime process admission.
    // Storage ownership is independent of canonical guest execution authority.
    internal bool VmStorageBackingEnabled { get; } = true;
}
