using System.Reflection;

namespace Erlang.NifNativeBackend;

public static class DeferredMailboxBackingCaseNames
{
    public const string OnHeap = "mailbox/deferred-on-heap-actual-value";
    public const string OffHeap = "mailbox/deferred-off-heap-actual-value";
    public const string ReleaseChangesMode = "mailbox/deferred-release-mode-revalidation";
    private const string GateField = "gate";

    internal static object Gate(object value)
        => value.GetType().GetField(GateField, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(value)!;
}
