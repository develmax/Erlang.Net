namespace Erlang;

internal sealed partial class RuntimeMailboxRootOwner593
{
    internal void RequirePreparedReadWork292(ProcessRuntime runtime, MailboxPayloadLease593 work)
    {
        lock (gate)
        {
            if (!ReferenceEquals(runtime.MailboxRoots593, this) || !ReferenceEquals(work.Owner593, this))
                throw new NotSupportedException(ProcessTerm506Names.ForeignRoot);
            ObjectDisposedException.ThrowIf(!work.Registered593 || !work.Published593
                || !ReferenceEquals(work.Link.List, active) || work.Ready || work.Executing, work);
        }
    }
}
