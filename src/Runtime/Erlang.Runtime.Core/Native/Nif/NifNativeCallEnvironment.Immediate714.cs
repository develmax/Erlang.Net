namespace Erlang;

internal sealed unsafe partial class NifNativeCallEnvironment
{
    internal static bool TryResolveImmediate714(nint environmentHint, out NifNativeCallEnvironment? owner)
    {
        // Exact managed index lookup only. Never dereference a native hint or
        // scan for an unrelated invocation/VM as its replacement.
        if (Environments.TryGetValue(environmentHint, out var candidate)
            && candidate.AccessibleOnCurrentThread && Volatile.Read(ref candidate.pointer) != 0)
        {
            owner = candidate;

            return true;
        }
        owner = null;

        return false;
    }
}
