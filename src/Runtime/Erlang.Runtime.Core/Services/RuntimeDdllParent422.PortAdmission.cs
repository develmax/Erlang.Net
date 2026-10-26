namespace Erlang;

internal sealed partial class RuntimeDdllParent422
{
    internal ValueTask<DdllParentRow422> AcquirePort(ProcessContext owner, ErlDriverName420 name, ulong port)
        => Stable(owner, () => AcquirePortCore(name, port));

    internal ValueTask<bool> CanOpen(ProcessContext owner, ErlDriverName420 name)
        => Stable(owner, () => Find(name) is { State: not DdllParentStateKind422.ForceReloading and not DdllParentStateKind422.ForceUnloading });
}
