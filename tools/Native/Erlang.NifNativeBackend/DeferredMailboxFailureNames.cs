namespace Erlang.NifNativeBackend;

public static class DeferredMailboxFailureNames
{
    public const string Prepare = "runtime/mailbox/deferred-prepare-release-complete-errors-drain";
    public const string Release = "runtime/mailbox/deferred-backed-release-complete-errors-drain";
    public const string Complete = "runtime/mailbox/deferred-backed-partial-complete-error-drain";
    internal const string Method = "PrepareAndCompleteDeferredSignal";
    internal const string Error = "Deferred mailbox failure lost cleanup, error identity or native backing";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(15);
}
