namespace Erlang;

public sealed partial class RuntimeDistributionPending
{
    internal bool HasUnicodeIo(string node)
    {
        lock (gate)
        {
            return entries.TryGetValue(node, out var weak) && weak.TryGetTarget(out var entry) &&
                entry.Connected is { } connected &&
                (connected.Flags & NetKernelNativeNames.UnicodeIoFlag) != 0;
        }
    }
}
